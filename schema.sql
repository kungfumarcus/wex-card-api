-- Reference schema (documentation only).
--
-- At runtime the skeleton builds this schema from the EF Core model in
-- AppDbContext via Database.EnsureCreated(). Once the model stabilises with
-- the currency-conversion features, EnsureCreated() is replaced by a proper
-- EF Core migration, which becomes the single source of truth for the schema.
--
-- Identifiers are quoted PascalCase to match EF Core's default naming.

CREATE TABLE "Cards" (
    "Id"          uuid           NOT NULL,
    "CreditLimit" numeric(19,4)  NOT NULL,
    "CreatedAt"   timestamptz    NOT NULL,
    CONSTRAINT "PK_Cards" PRIMARY KEY ("Id")
);

CREATE TABLE "Transactions" (
    "Id"              uuid          NOT NULL,
    "CardId"          uuid          NOT NULL,
    "Description"     varchar(200)  NOT NULL,
    "TransactionDate" date          NOT NULL,
    "Amount"          numeric(19,4) NOT NULL,
    "CreatedAt"       timestamptz   NOT NULL,
    CONSTRAINT "PK_Transactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Transactions_Cards_CardId"
        FOREIGN KEY ("CardId") REFERENCES "Cards" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Transactions_CardId" ON "Transactions" ("CardId");
