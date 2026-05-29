-- Employee ID issue date + education date columns (tvs-sqlscript should mirror).

ALTER TABLE zeloshr.zhr_employees
    ADD COLUMN IF NOT EXISTS id_issue_date DATE,
    ADD COLUMN IF NOT EXISTS id_expiry_date DATE;

ALTER TABLE zeloshr.zhr_employee_education
    ADD COLUMN IF NOT EXISTS start_date DATE,
    ADD COLUMN IF NOT EXISTS end_date DATE;

-- Backfill from legacy year columns when present.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'zeloshr' AND table_name = 'zhr_employee_education' AND column_name = 'start_year'
    ) THEN
        UPDATE zeloshr.zhr_employee_education
        SET start_date = make_date(start_year, 1, 1)
        WHERE start_date IS NULL AND start_year IS NOT NULL;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'zeloshr' AND table_name = 'zhr_employee_education' AND column_name = 'end_year'
    ) THEN
        UPDATE zeloshr.zhr_employee_education
        SET end_date = make_date(end_year, 12, 31)
        WHERE end_date IS NULL AND end_year IS NOT NULL;
    END IF;
END $$;
