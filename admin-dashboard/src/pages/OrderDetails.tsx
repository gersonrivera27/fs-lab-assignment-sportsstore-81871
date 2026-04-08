import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { apiUrl } from '../api';
import type { Order, OrderStatusDetails } from '../types';

export default function OrderDetails() {
  const { orderId } = useParams();
  const [order, setOrder] = useState<Order | null>(null);
  const [status, setStatus] = useState<OrderStatusDetails | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!orderId) return;

    Promise.all([
      fetch(apiUrl(`/api/orders/${orderId}`)).then(res => res.json()),
      fetch(apiUrl(`/api/orders/${orderId}/status`)).then(res => res.json()),
    ])
      .then(([orderData, statusData]) => {
        setOrder(orderData);
        setStatus(statusData);
        setLoading(false);
      })
      .catch(() => setLoading(false));
  }, [orderId]);

  if (loading) return <div className="loading">Loading order details...</div>;
  if (!order) return <div className="loading">Order not found.</div>;

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <div>
          <h2>Order Details</h2>
          <p style={{ color: 'var(--text-secondary)' }}>{order.orderId}</p>
        </div>
        <Link className="btn btn-secondary" to="/orders">Back to orders</Link>
      </div>

      <div className="details-grid">
        <section className="table-container details-panel">
          <h3>Overview</h3>
          <p><strong>Customer:</strong> {order.customerName}</p>
          <p><strong>Customer ID:</strong> {order.customerId}</p>
          <p><strong>Status:</strong> {order.status}</p>
          <p><strong>Total:</strong> ${order.totalAmount.toFixed(2)}</p>
          <p><strong>Correlation ID:</strong> {order.correlationId}</p>
          {order.failureReason && <p><strong>Failure:</strong> {order.failureReason}</p>}
        </section>

        <section className="table-container details-panel">
          <h3>Inventory</h3>
          {order.inventory ? (
            <>
              <p><strong>Outcome:</strong> {order.inventory.outcome}</p>
              <p><strong>Processed:</strong> {new Date(order.inventory.processedAt).toLocaleString()}</p>
              {order.inventory.failureReason && <p><strong>Reason:</strong> {order.inventory.failureReason}</p>}
            </>
          ) : <p>No inventory record yet.</p>}
        </section>

        <section className="table-container details-panel">
          <h3>Payment</h3>
          {order.payment ? (
            <>
              <p><strong>Outcome:</strong> {order.payment.outcome}</p>
              <p><strong>Amount:</strong> ${order.payment.amount.toFixed(2)}</p>
              <p><strong>Processed:</strong> {new Date(order.payment.processedAt).toLocaleString()}</p>
              {order.payment.transactionId && <p><strong>Transaction:</strong> {order.payment.transactionId}</p>}
              {order.payment.rejectionReason && <p><strong>Reason:</strong> {order.payment.rejectionReason}</p>}
            </>
          ) : <p>No payment record yet.</p>}
        </section>

        <section className="table-container details-panel">
          <h3>Shipping</h3>
          {order.shipment ? (
            <>
              <p><strong>Outcome:</strong> {order.shipment.outcome}</p>
              <p><strong>Created:</strong> {new Date(order.shipment.createdAt).toLocaleString()}</p>
              {order.shipment.shipmentReference && <p><strong>Reference:</strong> {order.shipment.shipmentReference}</p>}
              {order.shipment.estimatedDispatchDate && <p><strong>Dispatch:</strong> {new Date(order.shipment.estimatedDispatchDate).toLocaleString()}</p>}
              {order.shipment.failureReason && <p><strong>Reason:</strong> {order.shipment.failureReason}</p>}
            </>
          ) : <p>No shipment record yet.</p>}
        </section>
      </div>

      <section className="table-container details-panel" style={{ marginTop: '1.5rem' }}>
        <h3>Items</h3>
        <table>
          <thead>
            <tr>
              <th>Product</th>
              <th>Qty</th>
              <th>Unit Price</th>
              <th>Subtotal</th>
            </tr>
          </thead>
          <tbody>
            {order.items.map(item => (
              <tr key={`${item.productId}-${item.productName}`}>
                <td>{item.productName}</td>
                <td>{item.quantity}</td>
                <td>${item.unitPrice.toFixed(2)}</td>
                <td>${item.subtotal.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="table-container details-panel" style={{ marginTop: '1.5rem' }}>
        <h3>Workflow Timeline</h3>
        {status?.history?.length ? (
          <div className="timeline">
            {status.history.map(entry => (
              <div key={`${entry.status}-${entry.timestamp}`} className="timeline-item">
                <div className="timeline-status">{entry.status}</div>
                <div className="timeline-time">{new Date(entry.timestamp).toLocaleString()}</div>
                {entry.notes && <div className="timeline-notes">{entry.notes}</div>}
              </div>
            ))}
          </div>
        ) : (
          <p>No timeline available.</p>
        )}
      </section>
    </div>
  );
}
