<template>
  <VcWidget
    icon="lucide-arrow-left-right"
    :title="$t('RETURNS.WIDGETS.RETURNS')"
    :value="count"
    @click="open"
  />
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { injectBladeContext, useBlade } from "@vc-shell/framework";
import { VcWidget } from "@vc-shell/framework/ui";
import { searchReturns } from "../api";

// The AngularJS relatedReturnsWidget on customerOrderDetailWidgets: the order's returns, registered by blade name;
// the order comes from the Orders blade context.
const ctx = injectBladeContext();
const order = computed(() => ctx.value.item as { id?: string; number?: string } | undefined);
const { openBlade } = useBlade();
const count = ref<number>();

watch(
  () => order.value?.id,
  async (orderId) => {
    if (!orderId) return;
    count.value = (await searchReturns({ orderId, skip: 0, take: 0 }).catch(() => undefined))?.totalCount ?? 0;
  },
  { immediate: true },
);

const open = () =>
  openBlade({ name: "ReturnsList", options: { orderId: order.value?.id, orderNumber: order.value?.number } });
</script>
