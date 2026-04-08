import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { apiUrl } from '../api';
import type { Order } from '../types';

export default function FailedOrders() {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      fetch(apiUrl('/api/orders?status=Failed')).then(res => res.json()),
      fetch(apiUrl('/api/orders?status=PaymentFailed')).then(res => res.json()),
      fetch(apiUrl('/api/orders?status=InventoryFailed')).then(res => res.json()),
    ])
      .then(([failed, paymentFailed, inventoryFailed]) => {
        const combined = [...failed, ...paymentFailed, ...inventoryFailed] as Order[];
        const uniqueOrders = Array.from(new Map(combined.map(order => [order.orderId, order])).values());
        setOrders(uniqueOrders);
        setLoading(false);
      })
      .catch(() => setLoading(false));
  }, []);

  if (loading) return <div className="loading">Loading failed orders...</div>;

  return (
    <div>
      <div style={{ marginBottom: '2rem' }}>
        <h2>Failed Orders</h2>
        <p style={{ color: 'var(--text-secondary)' }}>
          Orders requiring operational attention across inventory, payment, or final workflow failures.
        </p>
      </div>

      <div className="table-container">
        <table>
          <thead>
            <tr>
              <th>Order ID</th>
              <th>Customer</th>
              <th>Status</th>
              <th>Failure Reason</th>
              <th>Created</th>
            </tr>
          </thead>
          <tbody>
            {orders.length === 0 ? (
              <tr>
                <td colSpan={5} style={{ textAlign: 'center', padding: '3rem' }}>No failed orders found.</td>
              </tr>
            ) : (
              orders.map(order => (
                <tr key={order.orderId}>
                  <td><Link className="table-link" to={`/orders/${order.orderId}`}>{order.orderId.substring(0, 8)}...</Link></td>
                  <td>{order.customerName}</td>
                  <td>{order.status}</td>
                  <td>{order.failureReason ?? order.payment?.rejectionReason ?? order.inventory?.failureReason ?? 'Unknown failure'}</td>
                  <td>{new Date(order.createdAt).toLocaleString()}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
