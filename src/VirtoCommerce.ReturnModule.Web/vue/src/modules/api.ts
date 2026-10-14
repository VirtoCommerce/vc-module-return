// Small same-origin client for what the generated client gets wrong — search is typed as a single Return, and update
// drops both the new return's id and the validation messages of a 400 — and for the order / member
// endpoints of sibling modules the blades read.
import { LocalizableSettingsClient, SecurityClient } from "@vc-shell/framework";
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import type { CustomerOrder, Return, ReturnSearchCriteria } from "../api_client/virtocommerce.return";

export interface SearchResult<T> {
  results?: T[];
  totalCount?: number;
}

async function request<T>(method: string, url: string, body?: unknown): Promise<T> {
  const response = await fetch(url, {
    method,
    credentials: "same-origin",
    headers: body === undefined ? { Accept: "application/json" } : { "Content-Type": "application/json", Accept: "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  if (!response.ok) {
    // the return endpoints answer a rejected edit with a list of messages
    let message = text || `${response.status} ${response.statusText}`;
    try {
      const parsed = JSON.parse(text);
      if (Array.isArray(parsed)) message = parsed.join("\n");
      else if (parsed?.message) message = parsed.message;
    } catch {
      /* not JSON */
    }
    throw new Error(message);
  }
  return (text ? JSON.parse(text) : undefined) as T;
}

export const searchReturns = (criteria: ReturnSearchCriteria) =>
  request<SearchResult<Return>>("POST", "/api/return/search", criteria);
/** creates (no id) or updates a return; answers the return's id */
export const saveReturn = (orderReturn: Return) => request<{ id?: string }>("PUT", "/api/return", orderReturn);

export const searchOrders = (criteria: Record<string, unknown>) =>
  request<SearchResult<CustomerOrder>>("POST", "/api/order/customerOrders/search", criteria);
export const getOrder = (id: string) =>
  request<CustomerOrder>("GET", `/api/order/customerOrders/${encodeURIComponent(id)}`);

export interface Member {
  id?: string;
  memberType?: string;
}
export const getMember = (id: string) => request<Member>("GET", `/api/members/${encodeURIComponent(id)}`);
export const getMemberByUserId = (userId: string) =>
  request<Member>("GET", `/api/members/accounts/${encodeURIComponent(userId)}`);
export const getUser = (userName: string) => new SecurityClient().getUserByName(userName);
/** a setting's allowed values (or its value): needs only a signed-in user, as the AngularJS settings.getValues */
export const getSettingValues = (name: string) =>
  request<unknown[]>("GET", `/api/platform/settings/values/${encodeURIComponent(name)}`);

/**
 * Labels of a localizable dictionary setting (Return.Status, Return.Reasons): the merchant's translation first,
 * then the label the module ships (RETURNS.<prefix>.<key>), then the key — as the AngularJS returnStatusTranslate.
 */
export function useDictionaryLabels(setting: string, prefix: string) {
  const { t, te, locale } = useI18n({ useScope: "global" });
  const translations = ref<Record<string, string>>({});

  // non-fatal: without translations the shipped labels show
  new LocalizableSettingsClient()
    .getDictionaryValues(setting, locale.value)
    .then((items) => {
      translations.value = Object.fromEntries(items.filter((x) => x.value && x.value !== x.key).map((x) => [x.key!, x.value!]));
    })
    .catch(() => undefined);

  return (key?: string) => {
    if (!key) return key ?? "";
    const own = `RETURNS.${prefix}.${key}`;
    return translations.value[key] ?? (te(own) ? t(own) : key);
  };
}

// VcStatus dot color per return status
const variants: Record<string, "info" | "warning" | "danger" | "success" | "primary"> = {
  New: "primary",
  Draft: "info",
  Requested: "warning",
  Processing: "warning",
  PartiallyApproved: "warning",
  Approved: "success",
  Completed: "success",
  Rejected: "danger",
  Canceled: "danger",
  Cancelled: "danger",
};
export const statusVariant = (status?: string) => variants[status ?? ""] ?? "info";
