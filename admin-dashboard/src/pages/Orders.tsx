import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { RefreshCw, Trash2 } from 'lucide-react';
import { apiUrl } from '../api';
import type { Order } from '../types';

export default function Orders() {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [cancelling, setCancelling] = useState<string | null>(null);
  const [selectedStatus, setSelectedStatus] = useState('');

  const fetchOrders = () => {
    setLoading(true);
    const url = selectedStatus
      ? apiUrl(`/api/orders?status=${encodeURIComponent(selectedStatus)}`)
      : apiUrl('/api/orders');

    fetch(url)
      .then(res => res.json())
      .then(data => {
        setOrders(data);
        setLoading(false);
      })
      .catch(err => {
        console.error("Failed to fetch orders", err);
        setLoading(false);
      });
  };

  useEffect(() => {
    fetchOrders();
    const interval = setInterval(fetchOrders, 5000); // Live refresh every 5 seconds
    return () => clearInterval(interval);
  }, [selectedStatus]);

  const handleCancelOrder = async (orderId: string) => {
    if (!confirm('Are you sure you want to cancel this order?')) return;
    
    setCancelling(orderId);
    try {
      const response = await fetch(apiUrl(`/api/orders/${orderId}`), {
        method: 'DELETE'
      });
      if (response.ok) {
        fetchOrders();
      } else {
        alert('Failed to cancel order.');
      }
    } catch (err) {
      console.error(err);
      alert('Error cancelling order.');
    } finally {
      setCancelling(null);
    }
  };

  const getStatusBadge = (status: string) => {
    const s = status.toLowerCase();
    let badgeClass = 'status-pending';
    
    if (s === 'completed') badgeClass = 'status-completed';
    else if (s.includes('failed') || s === 'cancelled') badgeClass = 'status-failed';
    else if (s === 'submitted' || s.includes('processing')) badgeClass = 'status-processing';
    
    return <span className={`badge ${badgeClass}`}>{status}</span>;
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <h2>Order Management</h2>
        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <select
            className="select-control"
            value={selectedStatus}
            onChange={e => setSelectedStatus(e.target.value)}
          >
            <option value="">All statuses</option>
            <option value="Submitted">Submitted</option>
            <option value="InventoryPending">InventoryPending</option>
            <option value="InventoryConfirmed">InventoryConfirmed</option>
            <option value="InventoryFailed">InventoryFailed</option>
            <option value="PaymentPending">PaymentPending</option>
            <option value="PaymentApproved">PaymentApproved</option>
            <option value="PaymentFailed">PaymentFailed</option>
            <option value="ShippingPending">ShippingPending</option>
            <option value="ShippingCreated">ShippingCreated</option>
            <option value="Completed">Completed</option>
            <option value="Failed">Failed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
          <button className="btn btn-primary" onClick={fetchOrders} disabled={loading}>
            <RefreshCw size={16} className={loading ? 'spinning' : ''} />
            {loading ? 'Refreshing...' : 'Refresh Orders'}
          </button>
        </div>
      </div>

      <div className="table-container">
        <div className="table-header">
          <h3 style={{ fontSize: '1rem' }}>All Recent Orders</h3>
        </div>
        <table>
          <thead>
            <tr>
              <th>Order ID</th>
              <th>Customer</th>
              <th>Date</th>
              <th>Total</th>
              <th>Status</th>
              <th style={{ textAlign: 'right' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {orders.length === 0 ? (
              <tr>
                <td colSpan={6} style={{ textAlign: 'center', padding: '3rem' }}>
                  {loading ? 'Loading...' : 'No orders found.'}
                </td>
              </tr>
            ) : (
              orders.map(order => (
              <tr key={order.orderId}>
                  <td>
                    <Link className="table-link" to={`/orders/${order.orderId}`}>
                      {order.orderId.substring(0, 8)}...
                    </Link>
                  </td>
                  <td>{order.customerName}</td>
                  <td>{new Date(order.createdAt).toLocaleString()}</td>
                  <td style={{ fontWeight: 600 }}>${order.totalAmount.toFixed(2)}</td>
                  <td>{getStatusBadge(order.status)}</td>
                  <td style={{ textAlign: 'right' }}>
                    <Link className="btn btn-secondary" to={`/orders/${order.orderId}`}>
                      Details
                    </Link>
                    {order.status !== 'Completed' && order.status !== 'Cancelled' && !order.status.includes('Failed') && (
                      <button 
                        className="btn btn-danger" 
                        onClick={() => handleCancelOrder(order.orderId)}
                        disabled={cancelling === order.orderId}
                      >
                        <Trash2 size={16} />
                        Cancel
                      </button>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
      <style>{`
        .spinning { animation: spin 1s linear infinite; }
        @keyframes spin { 100% { transform: rotate(360deg); } }
      `}</style>
    </div>
  );
}
