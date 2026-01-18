const http = require('http');

const options = {
    hostname: 'localhost',
    port: 5021,
    path: '/api/Sensor/history?hours=24',
    method: 'GET',
    headers: {
        'Accept': 'application/json'
    }
};

console.log('Testing API: http://localhost:5021/api/Sensor/history?hours=24\n');

const req = http.request(options, (res) => {
    console.log(`Status Code: ${res.statusCode}\n`);

    let data = '';

    res.on('data', (chunk) => {
        data += chunk;
    });

    res.on('end', () => {
        try {
            const jsonData = JSON.parse(data);
            console.log('Sample data (first 3 records):');
            jsonData.slice(0, 3).forEach((log, index) => {
                console.log(`\n${index + 1}. SensorId: ${log.sensorId}`);
                console.log(`   Value: ${log.value}`);
                console.log(`   Timestamp: ${log.timestamp}`);
                console.log(`   Parsed Date: ${new Date(log.timestamp).toLocaleString('th-TH')}`);
            });
            console.log(`\nTotal records: ${jsonData.length}`);
        } catch (e) {
            console.log('Raw Response:', data);
        }
    });
});

req.on('error', (error) => {
    console.error('Error:', error.message);
});

req.end();
