import { Outlet, NavLink } from 'react-router-dom';
import { LayoutDashboard, ShoppingCart, TriangleAlert } from 'lucide-react';

export default function Layout() {
  return (
    <div className="layout-container">
      <nav className="sidebar">
        <div className="sidebar-header">
          <ShoppingCart size={24} />
          SportsStore Admin
        </div>
        
        <div className="sidebar-nav">
          <NavLink 
            to="/" 
            className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
          >
            <LayoutDashboard size={20} />
            Dashboard
          </NavLink>
          
          <NavLink 
            to="/orders" 
            className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
          >
            <ShoppingCart size={20} />
            Orders
          </NavLink>
          <NavLink 
            to="/failed-orders" 
            className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
          >
            <TriangleAlert size={20} />
            Failed Orders
          </NavLink>
        </div>
      </nav>

      <main className="main-content">
        <Outlet />
      </main>
    </div>
  );
}
