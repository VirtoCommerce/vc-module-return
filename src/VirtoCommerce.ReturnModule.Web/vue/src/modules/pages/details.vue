<template>
  <VcBlade
    :title="title"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    :modified="form.isModified.value"
    width="50%"
  >
    <VcContainer>
      <VcForm class="returns-form">
        <div
          v-if="item.status"
          class="returns-state"
        >
          <VcStatus :variant="statusVariant(item.status)">{{ statusLabel(item.status) }}</VcStatus>
        </div>
        <VcRow class="returns-row">
          <VcCol>
            <VcInput
              :model-value="item.number"
              :label="$t('RETURNS.PAGES.DETAILS.FIELDS.NUMBER')"
              disabled
            />
          </VcCol>
          <VcCol>
            <VcInput
              :model-value="item.order?.number"
              :label="$t('RETURNS.PAGES.DETAILS.FIELDS.ORDER_NUMBER')"
              disabled
            />
          </VcCol>
        </VcRow>
        <VcRow class="returns-row">
          <VcCol>
            <VcField
              :label="$t('RETURNS.PAGES.DETAILS.FIELDS.CREATED_DATE')"
              :model-value="item.createdDate"
              type="date"
            />
          </VcCol>
          <VcCol>
            <VcField
              :label="$t('RETURNS.PAGES.DETAILS.FIELDS.MODIFIED_DATE')"
              :model-value="item.modifiedDate"
              type="date"
            />
          </VcCol>
        </VcRow>
        <VcRow class="returns-row">
          <VcCol>
            <div class="returns-link-field">
              <VcLabel>{{ $t("RETURNS.PAGES.DETAILS.FIELDS.CREATED_BY") }}</VcLabel>
              <VcLink
                v-if="item.createdBy"
                @click="openCreator"
                >{{ item.createdBy }}</VcLink
              >
              <span v-else>—</span>
            </div>
          </VcCol>
          <VcCol>
            <div class="returns-link-field">
              <VcLabel>{{ $t("RETURNS.PAGES.DETAILS.FIELDS.CUSTOMER") }}</VcLabel>
              <VcLink
                v-if="item.order?.customerName"
                @click="openCustomer"
                >{{ item.order.customerName }}</VcLink
              >
              <span v-else>—</span>
            </div>
          </VcCol>
        </VcRow>
        <VcRow class="returns-row">
          <VcCol>
            <div class="returns-status-field">
              <VcSelect
                v-model="item.status"
                class="returns-status-field__select"
                :label="$t('RETURNS.PAGES.DETAILS.FIELDS.STATUS')"
                :options="statusOptions"
                option-value="id"
                option-label="title"
                :clearable="false"
                :disabled="!canUpdate"
              />
              <VcButton
                v-if="canEditStatuses"
                icon="lucide-pencil"
                variant="ghost"
                size="sm"
                :aria-label="$t('RETURNS.PAGES.DETAILS.FIELDS.EDIT_STATUSES')"
                :title="$t('RETURNS.PAGES.DETAILS.FIELDS.EDIT_STATUSES')"
                @click="openStatuses"
              />
            </div>
          </VcCol>
          <VcCol>
            <VcInput
              :model-value="item.customerReference"
              :label="$t('RETURNS.PAGES.DETAILS.FIELDS.CUSTOMER_REFERENCE')"
              disabled
            />
          </VcCol>
        </VcRow>
        <VcTextarea
          v-model="item.resolution"
          :label="$t('RETURNS.PAGES.DETAILS.FIELDS.RESOLUTION')"
          :disabled="!canUpdate"
        />
        <VcTextarea
          :model-value="item.rejectReason"
          :label="$t('RETURNS.PAGES.DETAILS.FIELDS.REJECT_REASON')"
          disabled
        />
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  defineBladeContext,
  notification,
  useApiClient,
  useAsync,
  useBlade,
  useBladeForm,
  useBladeWidgets,
  useLoading,
  usePermissions,
  type IBladeToolbar,
} from "@vc-shell/framework";
import {
  VcBlade,
  VcButton,
  VcCol,
  VcContainer,
  VcField,
  VcForm,
  VcInput,
  VcLabel,
  VcLink,
  VcRow,
  VcSelect,
  VcStatus,
  VcTextarea,
} from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { getMember, getMemberByUserId, getUser, saveReturn, statusVariant, useDictionaryLabels, type Member } from "../api";
import { ReturnClient, type Return } from "../../api_client/virtocommerce.return";

defineBlade({
  url: "/return",
  name: "ReturnDetails",
  permissions: ["return:read"],
});

const { t } = useI18n({ useScope: "global" });
const { param, options, openBlade, callParent: callOwner, exposeToChildren } = useBlade<{ fromOrder?: boolean }>();
// opened from the order's "Create return": the parent is the order blade, whose reload would drop its unsaved edits
// (and a return doesn't change the order), so only a returns list parent is reloaded
const callParent = (method: string) => (options.value?.fromOrder ? Promise.resolve() : callOwner(method));
const { hasAccess } = usePermissions();
const { getApiClient } = useApiClient(ReturnClient);
const statusLabel = useDictionaryLabels("Return.Status", "STATUSES");

const canUpdate = computed(() => hasAccess("return:update"));
const canEditStatuses = computed(() => hasAccess(["platform:setting:access", "platform:setting:update"]));

const item = ref<Return>({});
const statuses = ref<string[]>([]);
const statusOptions = computed(() => statuses.value.map((x) => ({ id: x, title: statusLabel(x) })));

const form = useBladeForm({
  data: item,
  closeConfirmMessage: computed(() => t("RETURNS.ALERTS.CLOSE_CONFIRMATION")),
});

const title = computed(() => (item.value.number ? t("RETURNS.PAGES.DETAILS.TITLE", { number: item.value.number }) : ""));

// the return as last loaded: what differs from it are the user's unsaved edits
let loaded: Record<string, unknown> = {};

const { loading: loadLoading, action: load } = useAsync(async () => {
  const client = await getApiClient();
  // what an edit accepts depends on the return's decision as well as its status, so the server says
  const [result, available] = await Promise.all([
    client.getReturnById(param.value!),
    client.getAvailableStatuses(param.value!).catch(() => undefined),
  ]);
  item.value = result;
  form.setBaseline();
  loaded = JSON.parse(JSON.stringify(item.value));
  statuses.value = available ?? [item.value.status ?? ""];
});

// A child saved the return (lines, decision): re-read it and put the user's unsaved edits back on top, so a later
// Save neither drops them nor reverts what the child saved. A field both changed keeps the user's value.
async function reloadKeepingEdits() {
  const current = item.value as Record<string, unknown>;
  const keys = new Set([...Object.keys(current), ...Object.keys(loaded)]);
  const edits = [...keys].filter((k) => JSON.stringify(current[k]) !== JSON.stringify(loaded[k]));
  const values = Object.fromEntries(edits.map((k) => [k, current[k]]));
  await load();
  Object.assign(item.value, values);
}

const { loading: saveLoading, action: save } = useAsync(async () => {
  await saveReturn(item.value);
  notification.success(t("RETURNS.ALERTS.SAVED"), { timeout: 2500 });
  await load();
  await callParent("reload").catch(() => undefined);
});

const loading = useLoading(loadLoading, saveLoading);

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "refresh",
    title: t("RETURNS.PAGES.DETAILS.TOOLBAR.REFRESH"),
    icon: "lucide-refresh-cw",
    clickHandler: () => load(),
  },
  {
    id: "save",
    title: t("RETURNS.PAGES.DETAILS.TOOLBAR.SAVE"),
    icon: "lucide-save",
    permissions: "return:update",
    disabled: computed(() => !form.canSave.value),
    clickHandler: () => save(),
  },
  {
    id: "reset",
    title: t("RETURNS.PAGES.DETAILS.TOOLBAR.RESET"),
    icon: "lucide-undo-2",
    permissions: "return:update",
    disabled: computed(() => !form.isModified.value),
    clickHandler: () => form.revert(),
  },
]);

// the AngularJS pencil next to Status: the Return.Status dictionary, the list re-read when it is saved
const openStatuses = () => openBlade({ name: "PlatformSettingDictionary", param: "Return.Status" });

async function openMember(find: () => Promise<Member | undefined>) {
  const member = await find().catch(() => undefined);
  if (!member?.id) {
    notification.error(t("RETURNS.ALERTS.MEMBER_NOT_FOUND"), { timeout: 5000 });
    return;
  }
  const name = member.memberType === "Organization" ? "CustomerOrganizationDetails" : "CustomerContactDetails";
  // owned by the Customer plugin; it may not be installed in this host
  await openBlade({ name, param: member.id }).catch(() =>
    notification.error(t("RETURNS.ALERTS.MEMBER_UNAVAILABLE"), { timeout: 5000 }),
  );
}

const openCreator = () =>
  openMember(async () => {
    const user = await getUser(item.value.createdBy!);
    return user?.memberId ? getMember(user.memberId) : undefined;
  });

// the order's customer is a user id (storefront) or a contact id (created in the admin)
const openCustomer = () =>
  openMember(() => {
    const id = item.value.order!.customerId!;
    return getMemberByUserId(id).catch(() => getMember(id));
  });

const lineTotal = computed(() => {
  const total = (item.value.lineItems ?? []).reduce((sum, x) => sum + (x.quantity ?? 0) * (x.price ?? 0), 0);
  return `${Math.round((total + Number.EPSILON) * 100) / 100} ${item.value.order?.currency ?? ""}`.trim();
});

useBladeWidgets([
  {
    id: "ReturnItems",
    icon: "lucide-list",
    title: "RETURNS.WIDGETS.ITEMS",
    badge: computed(() => item.value.lineItems?.length ?? 0),
    onClick: () => openBlade({ name: "ReturnItems", options: { returnId: item.value.id } }),
  },
]);

// the return, for widgets other modules add to this blade
defineBladeContext({ item, lineTotal });

onMounted(load);

exposeToChildren({
  // the line items blade after a save / decision, the status dictionary after its save (unsaved edits are kept)
  reload: async () => {
    await (form.isModified.value ? reloadKeepingEdits() : load());
    await callParent("reload").catch(() => undefined);
  },
});
</script>

<style lang="scss">
.returns-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 20px 22px 36px;
}

.returns-row {
  gap: 16px;
}

.returns-state {
  display: flex;
  align-items: center;
}

.returns-link-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 14px;
}

.returns-status-field {
  display: flex;
  align-items: flex-end;
  gap: 4px;

  &__select {
    flex: 1;
    min-width: 0;
  }
}
</style>
