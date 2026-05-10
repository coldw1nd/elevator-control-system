<script setup lang="ts">
import type { UserDto } from '@/types/api';
import { formatDateTime, formatRole } from '@/utils/format';

defineProps<{
  users: UserDto[];
  currentUserId?: string | null;
}>();

defineEmits<{
  edit: [user: UserDto];
  toggleActivity: [user: UserDto];
}>();
</script>

<template>
  <v-table density="comfortable" class="rounded-lg border">
    <thead>
      <tr>
        <th>Логин</th>
        <th>Имя</th>
        <th>Роль</th>
        <th>Активность</th>
        <th>Создан</th>
        <th>Последний вход</th>
        <th class="text-right">Действия</th>
      </tr>
    </thead>

    <tbody>
      <tr v-for="user in users" :key="user.id">
        <td>{{ user.username }}</td>
        <td>{{ user.displayName }}</td>
        <td>{{ formatRole(user.role) }}</td>
        <td>
          <v-chip
            size="small"
            :color="user.isActive ? 'success' : 'error'"
            variant="tonal"
          >
            {{ user.isActive ? 'Активен' : 'Отключен' }}
          </v-chip>
        </td>
        <td>{{ formatDateTime(user.createdAtUtc) }}</td>
        <td>{{ formatDateTime(user.lastLoginAtUtc) }}</td>
        <td class="text-right">
          <v-btn
            variant="text"
            color="primary"
            icon="mdi-pencil-outline"
            @click="$emit('edit', user)"
          ></v-btn>

          <v-btn
            variant="text"
            :color="user.isActive ? 'warning' : 'success'"
            :icon="user.isActive ? 'mdi-account-off-outline' : 'mdi-account-check-outline'"
            :disabled="currentUserId === user.id && user.isActive"
            @click="$emit('toggleActivity', user)"
          ></v-btn>
        </td>
      </tr>

      <tr v-if="users.length === 0">
        <td colspan="7" class="text-center text-medium-emphasis py-6">
          Пользователи отсутствуют.
        </td>
      </tr>
    </tbody>
  </v-table>
</template>
