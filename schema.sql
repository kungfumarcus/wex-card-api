-- Reference schema (documentation only).
--
-- The InitialCreate EF Core migration (src/Wex.CardApi/Migrations) is the single
-- source of truth for the schema and is applied on startup via Database.Migrate().
-- This file is kept only as a human-readable reference; regenerate the canonical
-- SQL any time with:  dotnet ef migrations script -p src/Wex.CardApi
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
