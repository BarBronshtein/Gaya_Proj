# 0002: Dapper and Microsoft SQL Server Persistence

We decided to use Dapper as the high-performance Micro-ORM and Microsoft SQL Server as the relational database, deployed via Docker Compose. We avoided Entity Framework Core per user architectural requirement. Schema migrations, indexes, and seed records are managed via raw SQL scripts executed at startup, ensuring complete visibility, predictable SQL queries, and sub-millisecond execution times.
