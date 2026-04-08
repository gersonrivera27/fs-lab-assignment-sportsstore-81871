import { useEffect, useState } from 'react';
import { apiUrl } from '../api';
import type { DashboardSummary } from '../types';

export default function Dashboard() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch(apiUrl('/api/dashboard/summary'))
      .then(res => res.json())
      .then(data => {
        setSummary(data);
        setLoading(false);
      })
      .catch(err => {
        console.error("Failed to fetch dashboard summary", err);
        setLoading(false);
      });
  }, []);

  if (loading) return <div className="loading">Loading dashboard...</div>;

  return (
    <div>
      <h2 style={{ marginBottom: '2rem' }}>Dashboard Overview</h2>
      
      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-label">Total Revenue</div>
          <div className="stat-value" style={{ color: 'var(--success)' }}>
            ${summary?.totalRevenue.toFixed(2) || '0.00'}
          </div>
        </div>
        
        <div className="stat-card">
          <div className="stat-label">Total Orders</div>
          <div className="stat-value">{summary?.totalOrders || 0}</div>
        </div>
        
        <div className="stat-card">
          <div className="stat-label">Pending Orders</div>
          <div className="stat-value" style={{ color: 'var(--warning)' }}>
            {summary?.pendingOrders || 0}
          </div>
        </div>
        
        <div className="stat-card">
          <div className="stat-label">Failed Orders</div>
          <div className="stat-value" style={{ color: 'var(--danger)' }}>
            {summary?.failedOrders || 0}
          </div>
        </div>
      </div>

      <div className="table-container" style={{ padding: '1.5rem' }}>
        <h3 style={{ marginBottom: '1.5rem', color: 'var(--text-secondary)', fontSize: '1rem', textTransform: 'uppercase' }}>
          Orders By Status
        </h3>
        <div className="status-grid">
          {summary?.ordersByStatus.map(item => (
            <div key={item.status} className="status-card">
              <div className="status-card-label">{item.status}</div>
              <div className="status-card-value">{item.count}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
