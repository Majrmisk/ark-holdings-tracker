# PV260_notino_team3

## Prerequisites
- .NET 10
- Docker

## Technologies Used

**Backend:**
- .NET 10
- ASP.NET Core MVC
- PostgreSQL

**Data Access:**
- Marten (document store for holdings data)
- Entity Framework Core (ASP.NET Identity)
- CsvHelper (CSV parsing)

**Frontend:**
- Tailwind CSS
- Flowbite
- pnpm

**Testing:**
- xUnit
- Shouldly (assertions)
- Moq (mocking)
- Testcontainers (integration tests)
- WireMock.Net (HTTP mocking)

**Infrastructure:**
- Docker Compose
- Cronos (cron job scheduling)

## Running
1. start db container
```sh
docker compose up
```
2. run solution
```sh
cd Presentation/
dotnet run
```


## Diagrams

### Software design
![software design](software_design.png)

## Academic team project
This project was developed by a 5-member team for the PV260 course at Masaryk University (Spring 2026)

### Authors:
- 567816 Maros Pavlik
- 536407 Lubomir Jurcisin
- 536533 Martin Mackovic
- 536638 Marcel Nadzam
- 536589 Dominik Augustin
