# BarnCaseAPI

BarnCaseAPI is a full-stack web application that simulates farm management.  
Users can register an account, create farms, buy and sell animals, and manage the products those animals generate.  
Animals have limited lifespans and production cycles, making resource management part of the play.

---

## Features

- **User Accounts**
  - Register, log in, and authenticate with JWT.
  - Each user has a balance that updates when buying/selling.

- **Farm Management**
  - Create and delete farms.
  - List and select farms owned by the user.

- **Animals**
  - Buy and sell animals (cows, chickens, sheeps).
  - Animals have lifetimes and production intervals.
  - Selling price depends on purchase price and remaining life.

- **Products**
  - Animals produce resources (milk, eggs, wool).
  - Products can be sold for income.
  - Quantities are tracked and tied to farms.

- **Frontend**
  - Basic HTML + JavaScript pages (`app.html`, `signin.html`, `register.html`).
  - Interactive UI for farms, animals, products, and balance.

- **Backend**
  - ASP.NET Core Web API with Entity Framework Core + SQL Server.
  - Services handle domain logic (Users, Farms, Animals, Products, Production).
  - JWT authentication and refresh tokens.
  - Serilog for structured logging.

---

## Tech Stack

- **Backend:** ASP.NET Core (.NET 8), Entity Framework Core, SQL Server  
- **Frontend:** HTML, CSS, JavaScript (vanilla, no framework)  
- **Auth:** JWT with refresh tokens  
- **Logging:** Serilog

---
