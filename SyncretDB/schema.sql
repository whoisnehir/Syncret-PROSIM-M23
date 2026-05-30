CREATE TABLE loguri_proces (
    id SERIAL PRIMARY KEY,
    data_ora TIMESTAMP DEFAULT NOW(),
    componenta VARCHAR(100),
    tip_eveniment VARCHAR(50),
    mesaj TEXT
);