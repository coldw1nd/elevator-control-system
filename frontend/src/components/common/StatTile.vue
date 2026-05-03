<script setup lang="ts">
withDefaults(
  defineProps<{
    title: string;
    value: string | number;
    icon?: string;
    color?: string;
    subtitle?: string;
  }>(),
  {
    icon: 'mdi-chart-box',
    color: 'primary',
    subtitle: ''
  }
);
</script>

<template>
  <v-card class="stat-tile">
    <v-card-text class="d-flex align-start justify-space-between ga-4">
      <div>
        <div class="text-body-2 text-medium-emphasis">{{ title }}</div>
        <div class="text-h5 font-weight-bold mt-1">{{ value }}</div>
        <div v-if="subtitle" class="text-caption text-medium-emphasis mt-1">
          {{ subtitle }}
        </div>
      </div>

      <v-avatar :color="color" variant="tonal" size="48">
        <v-icon :color="color">{{ icon }}</v-icon>
      </v-avatar>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.stat-tile {
  height: 100%;
}
</style>
