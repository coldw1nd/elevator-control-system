<script setup lang="ts">
defineProps<{
  title: string;
  subtitle?: string;
}>();
</script>

<template>
  <v-card class="section-card">
    <v-card-item>
      <div class="d-flex align-start justify-space-between ga-4 flex-wrap">
        <div>
          <div class="text-h6 font-weight-bold">{{ title }}</div>
          <div v-if="subtitle" class="text-body-2 text-medium-emphasis mt-1">
            {{ subtitle }}
          </div>
        </div>

        <div class="d-flex ga-2 flex-wrap">
          <slot name="actions" />
        </div>
      </div>
    </v-card-item>

    <v-divider />

    <v-card-text>
      <slot />
    </v-card-text>
  </v-card>
</template>

<style scoped>
.section-card {
  border: 1px solid rgba(148, 163, 184, 0.16);
}
</style>
