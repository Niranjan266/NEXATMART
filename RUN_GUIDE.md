# How to Run the Online Grocery Shop Management System

Follow these steps to run the application on your local machine:

### 1. Prerequisites
- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) installed.

### 2. Run the Application
Open your terminal in the project directory (`d:\projects\online_gor`) and run:

```bash
dotnet run
```

**If the above doesn't work (command not found), try the full path:**
```bash
"C:\Program Files\dotnet\dotnet.exe" run
```

### 3. Access the System
Once the application starts, open your browser and navigate to:
[https://localhost:5001](https://localhost:5001) or the URL shown in the terminal.

### 4. Demo Credentials
- **Admin**: Username: `admin` | Password: `admin123`
- **Customer**: Username: `user` | Password: `user123`

### 5. Features Included
- **Premium Design**: Glassmorphism UI with smooth animations (Animate.css).
- **Responsive**: Works on desktop and mobile.
- **SQLite DB**: Database is automatically created and seeded on first run.
- **Cart System**: Browse products, add to cart, and checkout.
- **Admin Module**: Manage products and view customer orders.
