<script setup lang="ts">
withDefaults(
  defineProps<{
    title: string;
    description: string;
    icon?: string;
    actionText?: string;
    actionHref?: string;
  }>(),
  {
    icon: 'mdi-information-outline'
  }
);
</script>

<template>
  <v-card class="pa-8 text-center">
    <v-icon size="56" color="primary">{{ icon }}</v-icon>
    <div class="text-h6 font-weight-bold mt-4">{{ title }}</div>
    <div class="text-body-1 text-medium-emphasis mt-2">
      {{ description }}
    </div>

    <v-btn
      v-if="actionText && actionHref"
      class="mt-6"
      color="primary"
      variant="flat"
      :href="actionHref"
    >
      {{ actionText }}
    </v-btn>
  </v-card>
</template>
