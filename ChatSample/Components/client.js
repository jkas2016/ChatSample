document.getElementById('login-btn').addEventListener('click', function() {
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;

    axios.post('http://localhost:5129/login', {userName: username, password: password})
         .then(function (response) {
            if (response.status === 200 && response.data.token) {
                localStorage.setItem('token', response.data.token);
                document.getElementById('login-container').style.display = 'none';
                document.getElementById('chat-container').style.display = 'flex';
                initializeSignalR(response.data.token);
            } else {
                alert('Login failed');
            }
        })
        .catch(function (error) {
            console.error('Error logging in:', error);
            alert('An error occurred during login');
        });
});

function initializeSignalR(token) {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl('http://localhost:5129/chatHub', { accessTokenFactory: () => token })
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on('ReceiveMessage', (user, message) => {
        const chatBox = document.getElementById('chat-box');
        const messageElement = document.createElement('div');
        messageElement.textContent = `${user}: ${message}`;
        chatBox.appendChild(messageElement);
    });

    connection.start().catch(err => console.error('Connection failed: ', err));

    document.getElementById('send-btn').addEventListener('click', function() {
        const message = document.getElementById('chat-input').value;
        const username = document.getElementById('username').value;

        if (message.trim() !== '') {
            connection.invoke('SendMessage', message).catch(err => console.error(err.toString()));
            document.getElementById('chat-input').value = '';
        }
    });
}