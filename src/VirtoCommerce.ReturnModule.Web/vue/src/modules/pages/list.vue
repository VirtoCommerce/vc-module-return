<template>
  <VcBlade
    :title="$t('RETURNS.PAGES.LIST.TITLE')"
    :subtitle="orderNumber"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcDataTable
      v-model:search-value="searchValue"
      v-model:sort-field="sortField"
      v-model:sort-order="sortOrder"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      :state-key="stateKey"
      :active-item-id="activeChildParam"
      :show-all-columns="!activeChildParam"
      :global-filters="globalFilters"
      :row-actions="rowActions"
      row-actions-mode="dropdown"
      row-actions-position="column"
      :empty-state="{ icon: 'lucide-arrow-left-right', title: $t('RETURNS.PAGES.LIST.EMPTY') }"
      @row-click="(e: { data: Return }) => openDetails(e.data.id!)"
      @pagination-click="pagination.goToPage"
      @filter="onFilter"
    >
      <VcColumn
        id="number"
        :title="$t('RETURNS.PAGES.LIST.COLUMNS.NUMBER')"
        body-class="returns-list__number"
        mobile-role="title"
        sortable
        always-visible
      />
      <VcColumn
        id="orderNumber"
        :title="$t('RETURNS.PAGES.LIST.COLUMNS.ORDER_NUMBER')"
        mobile-role="field"
      >
        <template #body="{ data: r }">{{ r.order?.number }}</template>
      </VcColumn>
      <VcColumn
        id="customerName"
        :title="$t('RETURNS.PAGES.LIST.COLUMNS.CUSTOMER')"
        mobile-role="field"
      >
        <template #body="{ data: r }">{{ r.order?.customerName }}</template>
      </VcColumn>
      <VcColumn
        id="organizationName"
        :title="$t('RETURNS.PAGES.LIST.COLUMNS.ORGANIZATION')"
        sortable
      />
      <VcColumn
        id="status"
        :title="$t('RETURNS.PAGES.LIST.COLUMNS.STATUS')"
        mobile-role="status"
        sortable
        always-visible
      >
        <template #body="{ data: r }">
          <div class="tw-inline-flex">
            <VcStatus :variant="statusVariant(r.status)">{{ statusLabel(r.status) }}</VcStatus>
          </div>
        </template>
      </VcColumn>
      <VcColumn
        id="createdDate"
        :title="$t('RETURNS.PAGES.LIST.COLUMNS.CREATED_DATE')"
        type="date"
        mobile-role="field"
        sortable
      />
      <VcColumn
        id="modifiedDate"
        :title="$t('RETURNS.PAGES.LIST.COLUMNS.MODIFIED_DATE')"
        type="date"
        :visible="false"
        sortable
      />
      <VcColumn
        id="createdBy"
        :title="$t('RETURNS.PAGES.LIST.COLUMNS.CREATED_BY')"
        :visible="false"
        sortable
      />
      <VcColumn
        id="itemsCount"
        :title="$t('RETURNS.PAGES.LIST.COLUMNS.ITEMS_COUNT')"
        field="itemsCount"
        type="number"
        mobile-role="field"
        align="end"
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  notification,
  useAsync,
  useBlade,
  useDataTablePagination,
  useDataTableSort,
  useFunctions,
  useLatestRequest,
  useTableSearch,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { getSettingValues, searchReturns, statusVariant, useDictionaryLabels } from "../api";
import type { Return } from "../../api_client/virtocommerce.return";

defineBlade({
  url: "/returns",
  name: "ReturnsList",
  isWorkspace: true,
  permissions: ["return:access"],
  menuItem: {
    title: "RETURNS.MENU.TITLE",
    icon: "lucide-arrow-left-right",
    priority: 15,
  },
});

// Opened from an order's Returns widget the list shows that order's returns ("Create return" is on the order's toolbar).
const { options, activeChildParam, openBlade, exposeToChildren } = useBlade<{ orderId?: string; orderNumber?: string }>();
const orderId = computed(() => options.value?.orderId);
const orderNumber = computed(() => options.value?.orderNumber);
const stateKey = computed(() => (orderId.value ? "RETURNS_OF_ORDER" : "RETURNS"));

const { t } = useI18n({ useScope: "global" });
const { debounce } = useFunctions();
const statusLabel = useDictionaryLabels("Return.Status", "STATUSES");

const PAGE_SIZE = 20;
const items = ref<(Return & { itemsCount?: number })[]>([]);
const totalCount = ref(0);
const statusFilter = ref<string>();

const { sortField, sortOrder, sortExpression } = useDataTableSort({
  stateKey: stateKey.value,
  initialField: "number",
  initialDirection: "ASC",
});
const { searchValue } = useTableSearch({ stateKey: stateKey.value });
const pagination = useDataTablePagination({ stateKey: stateKey.value, pageSize: PAGE_SIZE, totalCount });
const search = useLatestRequest();

const { loading, action: load } = useAsync(async () => {
  const result = await search.latest(
    searchReturns({
      keyword: searchValue.value || undefined,
      statuses: statusFilter.value ? [statusFilter.value] : undefined,
      orderId: orderId.value,
      responseGroup: "WithOrders",
      sort: sortExpression.value,
      skip: pagination.skip,
      take: PAGE_SIZE,
    }),
  );
  if (!result) return;
  items.value = (result.results ?? []).map((r) => ({ ...r, itemsCount: r.lineItems?.length ?? 0 }));
  totalCount.value = result.totalCount ?? 0;
});

// Canceled is the spelling the module shipped with: it stays in the dictionary for the returns stored with it, and
// the search finds both spellings, so it is offered only where Cancelled is missing.
const statuses = ref<string[]>([]);
getSettingValues("Return.Status")
  .then((allowed) => {
    const values = (allowed ?? []).map(String);
    statuses.value = values.filter((x) => x !== "Canceled" || !values.includes("Cancelled"));
  })
  .catch(() => undefined);

const globalFilters = computed(() => [
  {
    id: "status",
    label: t("RETURNS.PAGES.LIST.FILTERS.STATUS"),
    filter: { options: statuses.value.map((s) => ({ value: s, label: statusLabel(s) })) },
  },
]);

function onFilter(event: { filters: Record<string, unknown> }) {
  const value = event.filters.status;
  statusFilter.value = (Array.isArray(value) ? value[0] : value) || undefined;
  reloadFirstPage();
}

function reloadFirstPage() {
  if (pagination.skip) pagination.setPage(1);
  else load();
}

load();
watch(searchValue, () => pagination.setPage(1));
watch([sortExpression, searchValue, () => pagination.skip], debounce(load, 300));

function openDetails(id: string) {
  openBlade({ name: "ReturnDetails", param: id });
}

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "refresh",
    title: t("RETURNS.PAGES.LIST.TOOLBAR.REFRESH"),
    icon: "lucide-refresh-cw",
    clickHandler: () => load(),
  },
  {
    id: "add",
    title: t("RETURNS.PAGES.LIST.TOOLBAR.ADD"),
    icon: "lucide-plus",
    permissions: "return:create",
    clickHandler: () => openBlade({ name: "ReturnOrders" }),
  },
]);

// execCommand, not navigator.clipboard: the admin runs over http on dev boxes (as the AngularJS list does)
function copy(text?: string) {
  if (!text) return;
  const area = document.createElement("textarea");
  area.value = text;
  document.body.appendChild(area);
  area.select();
  document.execCommand("copy");
  area.remove();
  notification.success(t("RETURNS.ALERTS.COPIED", { text }), { timeout: 2500 });
}

// the AngularJS row context menu
function rowActions(r: Return) {
  return [
    { id: "manage", icon: "lucide-pencil", title: t("RETURNS.PAGES.LIST.ACTIONS.MANAGE"), type: "info" as const, clickHandler: () => openDetails(r.id!) },
    { id: "copyId", icon: "lucide-copy", title: t("RETURNS.PAGES.LIST.ACTIONS.COPY_ID"), type: "info" as const, clickHandler: () => copy(r.id) },
    { id: "copyNumber", icon: "lucide-copy", title: t("RETURNS.PAGES.LIST.ACTIONS.COPY_NUMBER"), type: "info" as const, clickHandler: () => copy(r.number) },
  ];
}

exposeToChildren({
  reload: () => load(),
  // a return made from the order picker / line items opens next to the list (true: handled here)
  created: async (id: string) => {
    await load();
    openDetails(id);
    return true;
  },
});
</script>

<style lang="scss">
.returns-list__number {
  color: var(--color-neutral-900);
}
</style>
