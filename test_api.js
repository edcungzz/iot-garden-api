const http = require('http');

const options = {
    hostname: 'localhost',
    port: 5021,
    path: '/api/Sensor/list',
    method: 'GET',
    headers: {
        'Accept': 'application/json'
    }
};

console.log('Testing API: http://localhost:5021/api/Sensor/list\n');

const req = http.request(options, (res) => {
    console.log(`Status Code: ${res.statusCode}`);
    console.log(`Headers: ${JSON.stringify(res.headers, null, 2)}\n`);

    let data = '';

    res.on('data', (chunk) => {
        data += chunk;
    });

    res.on('end', () => {
        try {
            const jsonData = JSON.parse(data);
            console.log('Response Data:');
            console.log(JSON.stringify(jsonData, null, 2));
            console.log(`\nTotal sensors: ${jsonData.length}`);
        } catch (e) {
            console.log('Raw Response:', data);
        }
    });
});

req.on('error', (error) => {
    console.error('Error:', error.message);
});

req.end();
