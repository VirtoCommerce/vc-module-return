<template>
  <VcBlade
    :title="$t('RETURNS.PAGES.ITEMS.TITLE')"
    :subtitle="subtitle"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="70%"
  >
    <VcContainer>
      <div class="returns-items">
        <VcDataTable
          v-model:selection="selection"
          :selection-mode="editMode ? undefined : 'multiple'"
          :items="lines"
        >
          <VcColumn
            id="name"
            :title="$t('RETURNS.PAGES.ITEMS.COLUMNS.NAME')"
            mobile-role="title"
            always-visible
          >
            <template #body="{ data: line }">
              <div class="returns-line">
                <span
                  class="returns-line__name"
                  :title="line.name"
                  >{{ line.name }}</span
                >
                <span
                  v-if="line.sku"
                  class="returns-line__descr"
                  >{{ line.sku }}</span
                >
                <span
                  v-if="editMode && isMobile && (line.reasonCode || line.reason)"
                  class="returns-line__descr"
                  >{{ line.reasonCode ? reasonLabel(line.reasonCode) : line.reason }}</span
                >
              </div>
            </template>
          </VcColumn>
          <VcColumn
            id="reason"
            :title="$t('RETURNS.PAGES.ITEMS.COLUMNS.REASON')"
            :mobile-role="editMode ? undefined : 'field'"
          >
            <template #body="{ data: line }">
              <VcInput
                v-if="!editMode"
                v-model="line.reason"
                size="small"
                :label="isMobile ? $t('RETURNS.PAGES.ITEMS.COLUMNS.REASON') : undefined"
                :aria-label="$t('RETURNS.PAGES.ITEMS.COLUMNS.REASON')"
              />
              <div
                v-else
                class="returns-line"
              >
                <span>{{ line.reasonCode ? reasonLabel(line.reasonCode) : line.reason }}</span>
                <span
                  v-if="line.reasonComment"
                  class="returns-line__descr"
                  >{{ line.reasonComment }}</span
                >
                <span
                  v-if="line.serialNumber"
                  class="returns-line__descr"
                  >{{ $t("RETURNS.PAGES.ITEMS.SERIAL_NUMBER") }}: {{ line.serialNumber }}</span
                >
                <a
                  v-for="attachment in line.attachments ?? []"
                  :key="attachment.id ?? attachment.url"
                  class="returns-line__attachment"
                  :href="attachment.url"
                  target="_blank"
                  rel="noopener"
                  ><VcIcon
                    icon="lucide-paperclip"
                    size="xs"
                  />
                  {{ attachment.name }}</a
                >
              </div>
            </template>
          </VcColumn>
          <VcColumn
            id="quantity"
            :title="$t('RETURNS.PAGES.ITEMS.COLUMNS.QUANTITY')"
            mobile-role="field"
            width="96px"
          >
            <template #body="{ data: line }">
              <VcInput
                :model-value="line.quantity"
                type="integer"
                size="small"
                :label="isMobile ? $t('RETURNS.PAGES.ITEMS.COLUMNS.QUANTITY') : undefined"
                :aria-label="$t('RETURNS.PAGES.ITEMS.COLUMNS.QUANTITY')"
                :disabled="line.itemState === 'Approved' || line.itemState === 'Rejected'"
                @update:model-value="line.quantity = clamp($event, editMode ? 1 : 0, line.availableQuantity)"
              />
            </template>
          </VcColumn>
          <VcColumn
            v-if="editMode"
            id="approvedQuantity"
            :title="$t('RETURNS.PAGES.ITEMS.COLUMNS.APPROVED')"
            mobile-role="field"
            width="96px"
          >
            <template #body="{ data: line }">
              <VcInput
                v-if="canAuthorize"
                :model-value="line.approvedQuantity"
                type="integer"
                size="small"
                :label="isMobile ? $t('RETURNS.PAGES.ITEMS.COLUMNS.APPROVED') : undefined"
                :aria-label="$t('RETURNS.PAGES.ITEMS.COLUMNS.APPROVED')"
                :error="!validDecision(line)"
                @update:model-value="line.approvedQuantity = $event ?? undefined"
              />
              <span
                v-else
                class="vc-table-cell-number"
                >{{ line.approvedQuantity }}</span
              >
            </template>
          </VcColumn>
          <VcColumn
            v-if="editMode"
            id="rejectReason"
            :title="$t('RETURNS.PAGES.ITEMS.COLUMNS.REJECT_REASON')"
            mobile-role="field"
            width="140px"
          >
            <template #body="{ data: line }">
              <VcInput
                v-if="canAuthorize"
                v-model="line.rejectReason"
                size="small"
                :label="isMobile ? $t('RETURNS.PAGES.ITEMS.COLUMNS.REJECT_REASON') : undefined"
                :aria-label="$t('RETURNS.PAGES.ITEMS.COLUMNS.REJECT_REASON')"
                :disabled="(line.approvedQuantity ?? 0) >= (line.quantity ?? 0)"
              />
              <span v-else>{{ line.rejectReason }}</span>
            </template>
          </VcColumn>
          <VcColumn
            id="price"
            :title="$t('RETURNS.PAGES.ITEMS.COLUMNS.PRICE')"
            mobile-role="field"
            width="120px"
          >
            <template #body="{ data: line }">
              <VcInput
                v-if="canReadPrices"
                :model-value="line.price"
                type="number"
                step="0.01"
                size="small"
                :label="isMobile ? $t('RETURNS.PAGES.ITEMS.COLUMNS.PRICE') : undefined"
                :aria-label="$t('RETURNS.PAGES.ITEMS.COLUMNS.PRICE')"
                @update:model-value="line.price = Math.max(0, Number($event ?? 0))"
              />
              <span v-else>••••</span>
            </template>
          </VcColumn>
        </VcDataTable>
        <VcTextarea
          v-if="canAuthorize"
          v-model="entity.rejectReason"
          class="returns-items__reject"
          :label="$t('RETURNS.PAGES.ITEMS.REJECT_REASON')"
        />
      </div>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  notification,
  useApiClient,
  useAsync,
  useBlade,
  useLoading,
  usePermissions,
  useResponsive,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcContainer, VcDataTable, VcIcon, VcInput, VcTextarea } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { getOrder, saveReturn, useDictionaryLabels } from "../api";
import { ReturnClient, type Return, type ReturnLineItem } from "../../api_client/virtocommerce.return";

// The AngularJS items-list blade, in its two modes:
// - options.orderId: the order's lines to pick what is returned ("Make return" creates the return);
// - options.returnId: the return's lines — quantities and prices edited and saved, and while the return waits for a
//   decision (Requested / New) the approved quantity and decline reason of every line ("Approve / decline").
// Not routable: the order or return arrives only through options
defineBlade({
  url: "/return-items",
  name: "ReturnItems",
  permissions: ["return:read"],
  routable: false,
});

type Line = ReturnLineItem & { id?: string };

const { t } = useI18n({ useScope: "global" });
const { options, callParent, closeSelf, replaceWith } = useBlade<{ orderId?: string; returnId?: string }>();
const { hasAccess } = usePermissions();
const { getApiClient } = useApiClient(ReturnClient);
const reasonLabel = useDictionaryLabels("Return.Reasons", "REASONS");

const editMode = computed(() => !!options.value?.returnId);
// phones show a line as a card: the name with its SKU (and reason), then the labelled inputs
const { isMobile } = useResponsive();
const canReadPrices = hasAccess("order:read_prices");

const entity = ref<Return>({});
const original = ref<string>("");
const lines = ref<Line[]>([]);
const selection = ref<Line[]>([]);
const subtitle = ref("");
const canAuthorize = ref(false);

const clamp = (value: unknown, min: number, max?: number) => {
  const n = Number(value ?? min);
  return Math.min(max ?? Infinity, Math.max(min, Number.isFinite(n) ? n : min));
};

// An emptied field is not a decision to decline; a fraction is not a quantity.
const validDecision = (line: Line) =>
  Number.isInteger(line.approvedQuantity) && line.approvedQuantity! >= 0 && line.approvedQuantity! <= (line.quantity ?? 0);

// The decision is recorded by authorizing; saving never writes it.
function withoutDecision(r: Return) {
  const { order: _order, rejectReason: _reason, ...rest } = r;
  return JSON.stringify({
    ...rest,
    lineItems: (rest.lineItems ?? []).map(({ approvedQuantity: _a, rejectReason: _r, ...line }) => line),
  });
}

const { loading: loadLoading, action: load } = useAsync(async () => {
  const client = await getApiClient();
  if (editMode.value) {
    const result = await client.getReturnById(options.value!.returnId!);
    for (const line of result.lineItems ?? []) {
      const orderLine = result.order?.items?.find((x) => x.id === line.orderLineItemId);
      line.name ??= orderLine?.name;
      line.sku ??= orderLine?.sku;
    }
    // A requested return, or one raised here as New, is waiting for a decision. Until one is made, every line is
    // offered as approved in full, so declining is deliberate.
    canAuthorize.value = ["Requested", "New"].includes(result.status ?? "") && hasAccess("return:authorize");
    if (canAuthorize.value) result.lineItems?.forEach((line) => (line.approvedQuantity = line.quantity));
    entity.value = result;
    original.value = withoutDecision(result);
    lines.value = result.lineItems ?? [];
    subtitle.value = t("RETURNS.PAGES.ITEMS.SUBTITLE_EDIT", { number: result.number });
  } else {
    const orderId = options.value?.orderId;
    if (!orderId) return;
    const [order, available] = await Promise.all([getOrder(orderId), client.getAvailableQuantities(orderId)]);
    entity.value = { orderId, order };
    lines.value = (order.items ?? []).map((x) => ({
      id: x.id,
      orderLineItemId: x.id,
      name: x.name,
      sku: x.sku,
      price: x.price,
      quantity: available[x.id!] ?? 0,
      availableQuantity: available[x.id!] ?? 0,
      reason: undefined,
    }));
    subtitle.value = t("RETURNS.PAGES.ITEMS.SUBTITLE_CREATE", { number: order.number });
  }
  selection.value = [];
});

const toReturn = computed(() => selection.value.filter((x) => (x.quantity ?? 0) > 0));

const { loading: saveLoading, action: save } = useAsync(async () => {
  if (editMode.value) {
    await saveReturn(entity.value);
    notification.success(t("RETURNS.ALERTS.SAVED"), { timeout: 2500 });
    await callParent("reload").catch(() => undefined);
    await closeSelf();
    return;
  }
  const { id } = await saveReturn({
    orderId: entity.value.orderId,
    status: "New",
    lineItems: toReturn.value.map((line) => ({
      orderLineItemId: line.orderLineItemId,
      price: line.price,
      quantity: line.quantity,
      reason: line.reason || undefined,
    })),
  });
  // the returns list opens it next to itself; from the order's "Create return" (no such handler) it replaces this blade
  if (!(await callParent<boolean>("created", id)))
    await replaceWith({ name: "ReturnDetails", param: id, options: { fromOrder: true } });
});

const { loading: authorizeLoading, action: authorize } = useAsync(async () => {
  const client = await getApiClient();
  await client.authorizeReturn(entity.value.id!, {
    rejectReason: entity.value.rejectReason,
    items: lines.value.map((line) => ({
      lineItemId: line.id,
      approvedQuantity: line.approvedQuantity,
      rejectReason: line.rejectReason,
    })),
  });
  notification.success(t("RETURNS.ALERTS.AUTHORIZED"), { timeout: 2500 });
  await callParent("reload").catch(() => undefined);
  await load();
});

const loading = useLoading(loadLoading, saveLoading, authorizeLoading);

// the mode is fixed for the blade's life
const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "refresh",
    title: t("RETURNS.PAGES.ITEMS.TOOLBAR.REFRESH"),
    icon: "lucide-refresh-cw",
    clickHandler: () => load(),
  },
  editMode.value
    ? {
        id: "save",
        title: t("RETURNS.PAGES.ITEMS.TOOLBAR.SAVE"),
        icon: "lucide-save",
        permissions: "return:update",
        disabled: computed(() => !entity.value.id || withoutDecision(entity.value) === original.value),
        clickHandler: () => save(),
      }
    : {
        id: "makeReturn",
        title: t("RETURNS.PAGES.ITEMS.TOOLBAR.MAKE_RETURN"),
        icon: "lucide-arrow-left-right",
        permissions: "return:update",
        disabled: computed(() => !toReturn.value.length),
        clickHandler: () => save(),
      },
  {
    id: "authorize",
    title: t("RETURNS.PAGES.ITEMS.TOOLBAR.AUTHORIZE"),
    icon: "lucide-check",
    permissions: "return:authorize",
    isVisible: editMode.value,
    disabled: computed(() => !canAuthorize.value || !lines.value.every(validDecision)),
    clickHandler: () => authorize(),
  },
]);

onMounted(load);
</script>

<style lang="scss">
.returns-items {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 20px 22px 36px;
}

.returns-line {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;

  &__name {
    overflow: hidden;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    color: var(--color-neutral-900);
  }

  &__descr {
    font-size: 12px;
    color: var(--color-neutral-500);
  }

  &__attachment {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    font-size: 12px;
    color: var(--color-primary-500);
  }
}
</style>
