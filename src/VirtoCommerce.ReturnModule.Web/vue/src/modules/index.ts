import * as blades from "./pages";
import * as locales from "./locales";
import { defineAppModule, registerExternalWidget, useExtensionPoint, usePermissions } from "@vc-shell/framework";
import { markRaw, type App } from "vue";
import OrderReturnsWidget from "./widgets/OrderReturnsWidget.vue";
import type { OrdersDetailsToolbarCommand } from "./toolbar";
import { registerReturnsCounter } from "./menuCounter";

// "Create return" on the order blade's toolbar (legacy: makeReturnCommand on operationDetailController, shown on the
// order only, with return:create): the order's line items to pick what is returned
useExtensionPoint("orders-details-toolbar").add({
  id: "returns-create-return",
  priority: 6,
  component: markRaw({ render: () => null }),
  meta: {
    title: "RETURNS.PAGES.LIST.TOOLBAR.CREATE",
    icon: "lucide-arrow-left-right",
    permissions: "return:create",
    isVisible: (ctx) => !!ctx.order.id,
    onClick: (ctx) => ctx.openBlade({ name: "ReturnItems", options: { orderId: ctx.order.id } }),
  } satisfies OrdersDetailsToolbarCommand,
});

// The Returns module extends the Orders blade, as its AngularJS relatedReturnsWidget extends customerOrderDetailWidgets
// (shown with return:read; not on a new order).
registerExternalWidget({
  id: "ReturnsOrderReturns",
  title: "Returns",
  component: markRaw(OrderReturnsWidget),
  targetBlades: ["OrdersDetails"],
  isVisible: (blade) => !!blade?.param && usePermissions().hasAccess("return:read"),
});

const appModule = defineAppModule({
  blades,
  locales,
});

export default {
  install(app: App) {
    appModule.install(app);
    // returns requested on the menu item
    registerReturnsCounter(app);
  },
};
