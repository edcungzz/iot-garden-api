-- Add DeviceId column to SensorLogs table
ALTER TABLE "SensorLogs" ADD COLUMN "DeviceId" TEXT NULL;

-- Update existing records (optional - set to 'unknown' for old data)
UPDATE "SensorLogs" SET "DeviceId" = 'unknown' WHERE "DeviceId" IS NULL;
