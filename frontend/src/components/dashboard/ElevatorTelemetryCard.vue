<script setup lang="ts">
import { computed } from 'vue';
import type { SessionSnapshotDto } from '@/types/api';
import SectionCard from '@/components/common/SectionCard.vue';
import StatTile from '@/components/common/StatTile.vue';
import {
  formatDirection,
  formatMovementState,
  formatSessionStatus,
  formatWeight,
  getMovementStateColor,
  getSessionStatusColor
} from '@/utils/format';

const props = defineProps<{
  snapshot: SessionSnapshotDto | null;
}>();

const doorStatus = computed(() => {
  if (!props.snapshot) {
    return '—';
  }

  return props.snapshot.elevator.doorsAreOpen ? 'Открыты' : 'Закрыты';
});
</script>

<template>
  <SectionCard
    title="Текущее состояние лифта"
    subtitle="Ниже отображаются основные параметры кабины и состояние текущего сеанса."
  >
    <template v-if="snapshot">
      <div class="d-flex flex-wrap ga-3 mb-4">
        <v-chip
          :color="getSessionStatusColor(snapshot.status)"
          variant="tonal"
          prepend-icon="mdi-timeline-check"
        >
          {{ formatSessionStatus(snapshot.status) }}
        </v-chip>

        <v-chip
          :color="getMovementStateColor(snapshot.elevator.movementState)"
          variant="tonal"
          prepend-icon="mdi-elevator-passenger"
        >
          {{ formatMovementState(snapshot.elevator.movementState) }}
        </v-chip>

        <v-chip
          :color="snapshot.elevator.overloadIndicatorOn ? 'error' : 'secondary'"
          variant="tonal"
          prepend-icon="mdi-alert-circle-outline"
        >
          {{ snapshot.elevator.overloadIndicatorOn ? 'Перегрузка' : 'Перегрузка отсутствует' }}
        </v-chip>

        <v-chip
          v-if="snapshot.elevator.awaitingGoCommand"
          color="info"
          variant="tonal"
          prepend-icon="mdi-play-circle-outline"
        >
          Ожидает команду «Ход»
        </v-chip>

        <v-chip
          v-else-if="snapshot.elevator.goCommandPending"
          color="primary"
          variant="tonal"
          prepend-icon="mdi-check-circle-outline"
        >
          Команда «Ход» принята
        </v-chip>
      </div>

      <v-row>
        <v-col cols="12" md="6">
          <StatTile
            title="Текущий этаж"
            :value="snapshot.elevator.currentFloor"
            icon="mdi-numeric"
            color="primary"
          />
        </v-col>

        <v-col cols="12" md="6">
          <StatTile
            title="Текущая нагрузка"
            :value="formatWeight(snapshot.elevator.currentLoadKg)"
            icon="mdi-weight-kilogram"
            color="accent"
            :subtitle="`Максимум: ${formatWeight(snapshot.elevator.maxLoadKg)}`"
          />
        </v-col>

        <v-col cols="12" md="6">
          <StatTile
            title="Направление"
            :value="formatDirection(snapshot.elevator.direction)"
            icon="mdi-arrow-decision"
            color="success"
          />
        </v-col>

        <v-col cols="12" md="6">
          <StatTile
            title="Двери"
            :value="doorStatus"
            icon="mdi-door-sliding"
            color="info"
          />
        </v-col>

        <v-col cols="12" md="6">
          <StatTile
            title="Пассажиров в кабине"
            :value="snapshot.elevator.passengerCount"
            icon="mdi-account-group"
            color="warning"
          />
        </v-col>

        <v-col cols="12" md="6">
          <StatTile
            title="Сеанс"
            :value="snapshot.sessionName"
            icon="mdi-file-document-outline"
            color="secondary"
          />
        </v-col>
      </v-row>
    </template>

    <template v-else>
      <div class="text-body-2 text-medium-emphasis">
        Данные о состоянии лифта отсутствуют.
      </div>
    </template>
  </SectionCard>
</template>
