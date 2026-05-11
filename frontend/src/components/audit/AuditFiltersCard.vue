<script setup lang="ts">
import { ref } from 'vue';
import type { AuditLogQueryDto } from '@/types/api';
import SectionCard from '@/components/common/SectionCard.vue';

const props = defineProps<{
  busy: boolean;
}>();

const emit = defineEmits<{
  apply: [query: AuditLogQueryDto];
}>();

const username = ref('');
const action = ref('');
const entityType = ref('');
const sessionId = ref('');
const fromUtc = ref('');
const toUtc = ref('');
const limit = ref<number | null>(200);

function normalizeLimit(): number {
  const rawValue = Number(limit.value);

  if (!Number.isFinite(rawValue) || rawValue <= 0) {
    return 200;
  }

  return Math.min(1000, Math.trunc(rawValue));
}

function buildQuery(): AuditLogQueryDto {
  return {
    username: username.value.trim() || undefined,
    action: action.value.trim() || undefined,
    entityType: entityType.value.trim() || undefined,
    sessionId: sessionId.value.trim() || undefined,
    fromUtc: fromUtc.value ? new Date(fromUtc.value).toISOString() : undefined,
    toUtc: toUtc.value ? new Date(toUtc.value).toISOString() : undefined,
    limit: normalizeLimit()
  };
}

function applyFilters(): void {
  emit('apply', buildQuery());
}

function resetFilters(): void {
  username.value = '';
  action.value = '';
  entityType.value = '';
  sessionId.value = '';
  fromUtc.value = '';
  toUtc.value = '';
  limit.value = 200;

  applyFilters();
}

defineExpose({
  getCurrentQuery: buildQuery,
  resetFilters
});
</script>

<template>
  <SectionCard
    title="Фильтрация журнала аудита"
    subtitle="Можно отфильтровать записи по пользователю, действию, типу сущности, идентификатору сеанса и временному интервалу."
  >
    <v-row>
      <v-col cols="12" md="4">
        <v-text-field
          v-model="username"
          label="Пользователь"
          :disabled="props.busy"
        ></v-text-field>
      </v-col>

      <v-col cols="12" md="4">
        <v-text-field
          v-model="action"
          label="Действие"
          :disabled="props.busy"
        ></v-text-field>
      </v-col>

      <v-col cols="12" md="4">
        <v-text-field
          v-model="entityType"
          label="Тип сущности"
          :disabled="props.busy"
        ></v-text-field>
      </v-col>

      <v-col cols="12" md="4">
        <v-text-field
          v-model="sessionId"
          label="SessionId"
          :disabled="props.busy"
        ></v-text-field>
      </v-col>

      <v-col cols="12" md="4">
        <v-text-field
          v-model="fromUtc"
          label="Период с"
          type="datetime-local"
          :disabled="props.busy"
        ></v-text-field>
      </v-col>

      <v-col cols="12" md="4">
        <v-text-field
          v-model="toUtc"
          label="Период по"
          type="datetime-local"
          :disabled="props.busy"
        ></v-text-field>
      </v-col>

      <v-col cols="12" md="3">
        <v-text-field
          v-model.number="limit"
          label="Лимит записей"
          type="number"
          min="1"
          max="1000"
          :disabled="props.busy"
        ></v-text-field>
      </v-col>
    </v-row>

    <div class="d-flex justify-end ga-3 mt-2 flex-wrap">
      <v-btn variant="text" :disabled="props.busy" @click="resetFilters">Сбросить</v-btn>
      <v-btn
        color="primary"
        variant="flat"
        prepend-icon="mdi-filter-outline"
        :loading="props.busy"
        :disabled="props.busy"
        @click="applyFilters"
      >
        Применить фильтры
      </v-btn>
    </div>
  </SectionCard>
</template>
