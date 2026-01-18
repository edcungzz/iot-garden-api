const http = require('http');

async function testAPI(path, description) {
    return new Promise((resolve, reject) => {
        const options = {
            hostname: 'localhost',
            port: 5021,
            path: path,
            method: 'GET',
            headers: { 'Accept': 'application/json' }
        };

        console.log(`\n${'='.repeat(60)}`);
        console.log(`Testing: ${description}`);
        console.log(`URL: http://localhost:5021${path}`);
        console.log('='.repeat(60));

        const req = http.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => { data += chunk; });
            res.on('end', () => {
                try {
                    const jsonData = JSON.parse(data);
                    console.log(`Status: ${res.statusCode}`);

                    if (Array.isArray(jsonData)) {
                        console.log(`Total records: ${jsonData.length}`);
                        if (jsonData.length > 0) {
                            console.log('\nFirst record:');
                            console.log(JSON.stringify(jsonData[0], null, 2));
                        }
                    } else {
                        console.log('Response:');
                        console.log(JSON.stringify(jsonData, null, 2));
                    }
                    resolve();
                } catch (e) {
                    console.log('Raw Response:', data);
                    resolve();
                }
            });
        });

        req.on('error', (error) => {
            console.error('Error:', error.message);
            reject(error);
        });

        req.end();
    });
}

async function runTests() {
    try {
        await testAPI('/api/Sensor/history?hours=24', 'Sensor History (24 hours)');
        await testAPI('/api/Sensor/history?sensorId=heltec-v3-01_temp&hours=24', 'Temperature History');
        await testAPI('/api/Sensor/stats?hours=24', 'Sensor Statistics');
    } catch (error) {
        console.error('Test failed:', error);
    }
}

runTests();
