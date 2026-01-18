-- Add device_id column to sensor_logs table in Supabase
-- Run this in Supabase SQL Editor

-- Check if column already exists
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'sensor_logs' 
        AND column_name = 'device_id'
    ) THEN
        -- Add device_id column
        ALTER TABLE sensor_logs ADD COLUMN device_id TEXT;
        
        -- Add index for better query performance
        CREATE INDEX idx_sensor_logs_device_id ON sensor_logs(device_id);
        
        RAISE NOTICE 'Column device_id added successfully';
    ELSE
        RAISE NOTICE 'Column device_id already exists';
    END IF;
END $$;

-- Update existing records (optional - set to 'unknown' for old data)
-- UPDATE sensor_logs SET device_id = 'unknown' WHERE device_id IS NULL;

-- Verify
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'sensor_logs'
ORDER BY ordinal_position;
