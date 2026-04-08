export interface OrderItem {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface InventoryStatus {
  success: boolean;
  outcome: string;
  failureReason?: string | null;
  processedAt: string;
}

export interface PaymentStatus {
  approved: boolean;
  outcome: string;
  transactionId?: string | null;
  rejectionReason?: string | null;
  amount: number;
  processedAt: string;
}

export interface ShipmentStatus {
  success: boolean;
  outcome: string;
  shipmentReference?: string | null;
  estimatedDispatchDate?: string | null;
  failureReason?: string | null;
  createdAt: string;
}

export interface Order {
  orderId: string;
  customerId: string;
  customerName: string;
  status: string;
  totalAmount: number;
  createdAt: string;
  updatedAt?: string | null;
  items: OrderItem[];
  shipmentReference?: string | null;
  trackingInfo?: string | null;
  failureReason?: string | null;
  inventory?: InventoryStatus | null;
  payment?: PaymentStatus | null;
  shipment?: ShipmentStatus | null;
  correlationId: string;
}

export interface DashboardSummary {
  totalOrders: number;
  pendingOrders: number;
  completedOrders: number;
  failedOrders: number;
  totalRevenue: number;
  ordersByStatus: { status: string; count: number }[];
}

export interface OrderStatusHistory {
  status: string;
  timestamp: string;
  notes?: string | null;
}

export interface OrderStatusDetails {
  orderId: string;
  status: string;
  lastUpdated: string;
  shipmentReference?: string | null;
  failureReason?: string | null;
  history: OrderStatusHistory[];
}
