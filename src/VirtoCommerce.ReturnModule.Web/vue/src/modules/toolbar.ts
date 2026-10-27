// The Orders order blade's toolbar extension point "orders-details-toolbar" (contract documented in the Orders module,
// vc-module-order .../vue/src/modules/pages/details.vue). Types repeated here: the contract is by shape.

export interface OrdersDetailsToolbarContext {
  order: { id?: string; number?: string };
  locked: boolean;
  openBlade: (event: { name: string; param?: string; options?: Record<string, unknown> }) => Promise<void>;
}

export interface OrdersDetailsToolbarCommand {
  title: string;
  icon?: string;
  permissions?: string | string[];
  isVisible?: (ctx: OrdersDetailsToolbarContext) => boolean;
  disabled?: (ctx: OrdersDetailsToolbarContext) => boolean;
  onClick: (ctx: OrdersDetailsToolbarContext) => unknown;
}
