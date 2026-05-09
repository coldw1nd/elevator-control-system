<script setup lang="ts">
import { computed } from 'vue';
import type { SessionReportDto } from '@/types/api';
import SectionCard from '@/components/common/SectionCard.vue';
import StatTile from '@/components/common/StatTile.vue';
import { formatDateTime, formatWeight } from '@/utils/format';

const props = defineProps<{
  report: SessionReportDto | null;
}>();

const deliveredCount = computed(() => {
  if (!props.report) {
    return 0;
  }

  return props.report.passengers.filter(
    (passenger) => passenger.status === 'Delivered' || passenger.status === 'Archived'
  ).length;
});

const archivedCount = computed(() => {
  if (!props.report) {
    return 0;
  }

  return props.report.passengers.filter((passenger) => passenger.status === 'Archived').length;
});
</script>

<template>
  <SectionCard
    title="Итоговый отчёт по сеансу"
    subtitle="Финальные показатели сеанса формируются после корректной остановки системы."
  >
    <template v-if="report">
      <v-row>
        <v-col cols="12" md="6" xl="3">
          <StatTile
            title="Общее количество поездок"
            :value="report.totalTrips"
            icon="mdi-swap-vertical-bold"
            color="primary"
          />
        </v-col>

        <v-col cols="12" md="6" xl="3">
          <StatTile
            title="Холостые поездки"
            :value="report.emptyTrips"
            icon="mdi-truck-fast-outline"
            color="warning"
          />
        </v-col>

        <v-col cols="12" md="6" xl="3">
          <StatTile
            title="Суммарный перемещённый вес"
            :value="formatWeight(report.totalTransportedWeightKg)"
            icon="mdi-weight-lifter"
            color="accent"
          />
        </v-col>

        <v-col cols="12" md="6" xl="3">
          <StatTile
            title="Создано объектов «человек»"
            :value="report.totalCreatedPassengers"
            icon="mdi-account-multiple-plus"
            color="success"
          />
        </v-col>

        <v-col cols="12" md="6">
          <StatTile
            title="Доставленные пассажиры"
            :value="deliveredCount"
            icon="mdi-account-check-outline"
            color="success"
          />
        </v-col>

        <v-col cols="12" md="6">
          <StatTile
            title="Архивированные пассажиры"
            :value="archivedCount"
            icon="mdi-archive-outline"
            color="secondary"
          />
        </v-col>
      </v-row>

      <v-divider class="my-4"></v-divider>

      <v-row>
        <v-col cols="12" md="4">
          <div class="text-body-2 text-medium-emphasis">Название сеанса</div>
          <div class="text-body-1 font-weight-medium mt-1">{{ report.sessionName }}</div>
        </v-col>

        <v-col cols="12" md="4">
          <div class="text-body-2 text-medium-emphasis">Время запуска</div>
          <div class="text-body-1 font-weight-medium mt-1">
            {{ formatDateTime(report.sessionStartedAtUtc) }}
          </div>
        </v-col>

        <v-col cols="12" md="4">
          <div class="text-body-2 text-medium-emphasis">Время остановки</div>
          <div class="text-body-1 font-weight-medium mt-1">
            {{ formatDateTime(report.sessionStoppedAtUtc) }}
          </div>
        </v-col>

        <v-col cols="12">
          <div class="text-body-2 text-medium-emphasis">Отчёт сформирован</div>
          <div class="text-body-1 font-weight-medium mt-1">
            {{ formatDateTime(report.generatedAtUtc) }}
          </div>
        </v-col>
      </v-row>
    </template>

    <template v-else>
      <div class="text-body-2 text-medium-emphasis">
        Отчёт по выбранному сеансу пока не загружен.
      </div>
    </template>
  </SectionCard>
</template>
