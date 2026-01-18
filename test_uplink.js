const http = require('http');

// Simulate The Things Stack payload
const payload = {
    "end_device_ids": {
        "device_id": "heltec-v3-01"
    },
    "uplink_message": {
        "decoded_payload": {
            "temp": 27.5,
            "humi": 88.2,
            "ec": 0,
            "ph": 4.3,
            "n": 0,
            "p": 7,
            "k": 0
        }
    }
};

const data = JSON.stringify(payload);

const options = {
    hostname: 'localhost',
    port: 5021,
    path: '/api/uplink',
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'Content-Length': data.length
    }
};

console.log('Sending test uplink message...\n');
console.log('Payload:', JSON.stringify(payload, null, 2));

const req = http.request(options, (res) => {
    console.log(`\nStatus Code: ${res.statusCode}`);

    let responseData = '';

    res.on('data', (chunk) => {
        responseData += chunk;
    });

    res.on('end', () => {
        console.log('Response:', responseData);
        console.log('\n✅ Test completed! Check Supabase for new data with Thailand timezone.');
    });
});

req.on('error', (error) => {
    console.error('Error:', error.message);
});

req.write(data);
req.end();
