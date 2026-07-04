
---

# 🛒 NEXATMART – Voice-Powered Grocery Shopping for the Blind

**NEXATMART** is an inclusive **Online Grocery Shopping Web Application** designed primarily for **blind and visually impaired users**. The platform enables customers to order groceries easily using **voice commands**, ensuring accessibility, independence, and convenience.  

---

## 🌟 Key Features
- 🎙️ **Voice Ordering System** – Users can browse and order groceries using voice commands  
- 🛍️ **Product Catalog** – Organized grocery items with categories  
- 🛒 **Shopping Cart** – Add, update, and remove items seamlessly  
- 📦 **Order Management** – Place and track orders with voice assistance  
- 👤 **User Authentication** – Secure login and registration system  
- ⚙️ **Admin Dashboard** – Manage products, users, and orders  
- 🎨 **Accessible UI** – Designed with screen reader compatibility and responsive layout  

---

## 🛠️ Technologies Used
- **ASP.NET Core MVC (C#)** – Backend framework  
- **Entity Framework Core** – ORM for database operations  
- **SQLite** – Database for storing products, users, and orders  
- **Razor Views** – Dynamic frontend rendering  
- **HTML5, CSS3, JavaScript** – UI and interactivity  
- **Voice Recognition API** – For voice-based grocery ordering  
- **VS Code / Visual Studio** – Development environment  
- **GitHub** – Version control and collaboration  

---

## 📂 Project Structure
```
NEXATMART/
│── OnlineGroceryShop.csproj    # Project configuration file
│── Program.cs                  # Application entry point
│── appsettings.json            # Application settings (DB connection, etc.)
│── grocery.db                  # SQLite database file
│── RUN_GUIDE.md                # Instructions to run the project
│── README.md                   # Documentation
│── online_grocery--main.sln    # Solution file
│── git                         # Git-related file
│
├── .vscode/                    # VS Code workspace settings
│
├── Controllers/                # MVC Controllers
│   ├── HomeController.cs
│   ├── ProductsController.cs
│   ├── CartController.cs
│   ├── OrdersController.cs
│   └── UsersController.cs
│
├── Data/                       # Database context and migrations
│   ├── ApplicationDbContext.cs
│   └── Migrations/             # EF Core migration files
│
├── Models/                     # Data models
│   ├── Product.cs
│   ├── User.cs
│   ├── Order.cs
│   └── CartItem.cs
│
├── Views/                      # Razor Views (UI templates)
│   ├── Shared/                 # Shared layouts and partials
│   │   └── _Layout.cshtml
│   ├── Home/
│   │   └── Index.cshtml
│   ├── Products/
│   │   └── List.cshtml
│   ├── Cart/
│   │   └── Index.cshtml
│   ├── Orders/
│   │   └── Checkout.cshtml
│   └── Users/
│       ├── Login.cshtml
│       └── Register.cshtml
│
├── wwwroot/                    # Static files
│   ├── css/
│   │   └── site.css
│   ├── js/
│   │   └── site.js
│   └── images/
│       └── logo.png
│
├── bin/Debug/                  # Build output
└── obj/                        # Build cache
```

---

## 📖 Getting Started

### 1. Clone the Repository
```bash
git clone https://github.com/Niranjan266/NEXATMART.git
cd NEXATMART
```

### 2. Install Dependencies
Ensure you have **.NET SDK** installed.  
Restore packages:
```bash
dotnet restore
```

### 3. Run the Application
```bash
dotnet run
```
Open your browser and navigate to **`http://localhost:5000/`**

---

## 📸 Accessibility Highlights
- **Voice Ordering** – Blind users can order groceries without needing to navigate visually  
- **Screen Reader Support** – Pages are optimized for assistive technologies  
- **Minimal Visual Dependency** – Core actions can be performed via voice  

---

## 📌 Future Enhancements
- 🔐 Role-based access (Admin vs Customer)  
- 📈 Analytics dashboard for sales reports  
- ☁️ Cloud deployment (Azure/AWS)  
- 💳 Payment gateway integration  
- 🌍 Multi-language voice support  

---

## 👨‍💻 Author
Developed by **Niranjan266**  
GitHub Profile: Niranjan266 [(github.com)](https://www.bing.com/search?q="https%3A%2F%2Fgithub.com%2FNiranjan266")

---
