<template>
  <VcBlade
    :title="$t('RETURNS.PAGES.ORDERS.TITLE')"
    :subtitle="$t('RETURNS.PAGES.ORDERS.SUBTITLE')"
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
      state-key="RETURNS_ORDERS"
      :active-item-id="activeItemId"
      :empty-state="{ icon: 'lucide-file-text', title: $t('RETURNS.PAGES.ORDERS.EMPTY') }"
      @row-click="(e: { data: CustomerOrder }) => openItems(e.data.id!)"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="number"
        :title="$t('RETURNS.PAGES.ORDERS.COLUMNS.NUMBER')"
        mobile-role="title"
        sortable
        always-visible
      />
      <VcColumn
        id="customerName"
        :title="$t('RETURNS.PAGES.ORDERS.COLUMNS.CUSTOMER')"
        mobile-role="field"
        sortable
      />
      <VcColumn
        id="storeId"
        :title="$t('RETURNS.PAGES.ORDERS.COLUMNS.STORE')"
        sortable
      />
      <VcColumn
        id="total"
        :title="$t('RETURNS.PAGES.ORDERS.COLUMNS.TOTAL')"
        mobile-role="field"
        align="end"
        sortable
      >
        <template #body="{ data: o }">
          <span class="vc-table-cell-money">{{ canReadPrices ? money(o.total, o.currency) : "••••" }}</span>
        </template>
      </VcColumn>
      <VcColumn
        id="currency"
        :title="$t('RETURNS.PAGES.ORDERS.COLUMNS.CURRENCY')"
        :visible="false"
        sortable
      />
      <VcColumn
        id="isApproved"
        :title="$t('RETURNS.PAGES.ORDERS.COLUMNS.CONFIRMED')"
        :visible="false"
        sortable
      >
        <template #body="{ data: o }">{{ o.isApproved ? $t("RETURNS.YES") : $t("RETURNS.NO") }}</template>
      </VcColumn>
      <VcColumn
        id="status"
        :title="$t('RETURNS.PAGES.ORDERS.COLUMNS.STATUS')"
        mobile-role="status"
        sortable
      >
        <template #body="{ data: o }">
          <div class="tw-inline-flex">
            <VcStatus :variant="statusVariant(o.status)">{{ o.status }}</VcStatus>
          </div>
        </template>
      </VcColumn>
      <VcColumn
        id="createdDate"
        :title="$t('RETURNS.PAGES.ORDERS.COLUMNS.CREATED')"
        type="date"
        mobile-role="field"
        sortable
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  useAsync,
  useBlade,
  useDataTablePagination,
  useDataTableSort,
  useFunctions,
  useLatestRequest,
  usePermissions,
  useTableSearch,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { searchOrders, statusVariant } from "../api";
import type { CustomerOrder } from "../../api_client/virtocommerce.return";

// The AngularJS order-list blade of the Returns module: pick the order a new return is made from.
// ponytail: keyword search only; the Orders module's saved filters live in that module's own storage.
defineBlade({
  url: "/return-orders",
  name: "ReturnOrders",
  permissions: ["return:create"],
});

const { t, n } = useI18n({ useScope: "global" });
const { openBlade, callParent, exposeToChildren } = useBlade();
const { debounce } = useFunctions();
const { hasAccess } = usePermissions();
const canReadPrices = hasAccess("order:read_prices");
const money = (value?: number, currency?: string) =>
  `${n(value ?? 0, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${currency ?? ""}`.trim();

const PAGE_SIZE = 20;
const items = ref<CustomerOrder[]>([]);
const totalCount = ref(0);
const activeItemId = ref<string>();

const { sortField, sortOrder, sortExpression } = useDataTableSort({
  stateKey: "RETURNS_ORDERS",
  initialField: "createdDate",
  initialDirection: "DESC",
});
const { searchValue } = useTableSearch({ stateKey: "RETURNS_ORDERS" });
const pagination = useDataTablePagination({ stateKey: "RETURNS_ORDERS", pageSize: PAGE_SIZE, totalCount });
const search = useLatestRequest();

const { loading, action: load } = useAsync(async () => {
  const result = await search.latest(
    searchOrders({
      responseGroup: "WithPrices",
      keyword: searchValue.value || undefined,
      sort: sortExpression.value,
      skip: pagination.skip,
      take: PAGE_SIZE,
    }),
  );
  if (!result) return;
  items.value = result.results ?? [];
  totalCount.value = result.totalCount ?? 0;
});

load();
watch(searchValue, () => pagination.setPage(1));
watch([sortExpression, searchValue, () => pagination.skip], debounce(load, 300));

function openItems(id: string) {
  activeItemId.value = id;
  openBlade({ name: "ReturnItems", options: { orderId: id }, onClose: () => (activeItemId.value = undefined) });
}

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("RETURNS.PAGES.LIST.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
]);

// the new return opens next to the list, in the picker's place
exposeToChildren({ created: (id: string) => callParent("created", id) });
</script>
