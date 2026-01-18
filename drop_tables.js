const { Client } = require('pg');

const connectionString = "postgresql://postgres.fjljkiwiobhbazzqusie:osWDtHrdSWQ96Z29@aws-1-ap-southeast-1.pooler.supabase.com:6543/postgres";

async function dropTables() {
    const client = new Client({
        connectionString: connectionString,
        ssl: { rejectUnauthorized: false }
    });

    try {
        await client.connect();
        console.log('🔌 Connected to database. Fetching all tables...');

        const res = await client.query(`
            SELECT tablename 
            FROM pg_tables 
            WHERE schemaname = 'public'
        `);

        if (res.rows.length === 0) {
            console.log('✅ No tables found to drop.');
            return;
        }

        console.log(`Found ${res.rows.length} tables. Dropping...`);

        for (const row of res.rows) {
            const tableName = row.tablename;
            console.log(`Dropping table: ${tableName}`);
            await client.query(`DROP TABLE IF EXISTS "${tableName}" CASCADE`);
        }

        console.log('✅ All public tables dropped successfully.');
    } catch (err) {
        console.error('❌ Error dropping tables:', err);
    } finally {
        await client.end();
    }
}

dropTables();
