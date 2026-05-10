<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type {
  CreateUserRequestDto,
  UpdateUserRequestDto,
  UserDto,
  UserRole
} from '@/types/api';

const visible = defineModel<boolean>({
  required: true
});

const props = withDefaults(
  defineProps<{
    mode: 'create' | 'edit';
    user?: UserDto | null;
    busy?: boolean;
  }>(),
  {
    user: null,
    busy: false
  }
);

const emit = defineEmits<{
  save: [request: CreateUserRequestDto | UpdateUserRequestDto];
}>();

const roleItems: { title: string; value: UserRole }[] = [
  {
    title: 'Администратор',
    value: 'Admin'
  },
  {
    title: 'Оператор',
    value: 'Operator'
  },
  {
    title: 'Наблюдатель',
    value: 'Viewer'
  }
];

const username = ref('');
const displayName = ref('');
const password = ref('');
const role = ref<UserRole>('Viewer');
const isActive = ref(true);

function syncForm(): void {
  if (props.mode === 'edit' && props.user) {
    username.value = props.user.username;
    displayName.value = props.user.displayName;
    password.value = '';
    role.value = props.user.role;
    isActive.value = props.user.isActive;
    return;
  }

  username.value = '';
  displayName.value = '';
  password.value = '';
  role.value = 'Viewer';
  isActive.value = true;
}

watch(
  () => [visible.value, props.mode, props.user],
  () => {
    if (visible.value) {
      syncForm();
    }
  },
  {
    immediate: true
  }
);

const validationMessage = computed(() => {
  if (username.value.trim().length < 3 || username.value.trim().length > 50) {
    return 'Логин должен содержать от 3 до 50 символов.';
  }

  if (displayName.value.trim().length < 2 || displayName.value.trim().length > 100) {
    return 'Отображаемое имя должно содержать от 2 до 100 символов.';
  }

  if (props.mode === 'create' && password.value.trim().length < 8) {
    return 'Пароль нового пользователя должен содержать не менее 8 символов.';
  }

  if (props.mode === 'edit' && password.value.trim().length > 0 && password.value.trim().length < 8) {
    return 'Новый пароль должен содержать не менее 8 символов.';
  }

  return '';
});

const canSave = computed(() => validationMessage.value.length === 0 && !props.busy);

function handleSave(): void {
  if (!canSave.value) {
    return;
  }

  if (props.mode === 'create') {
    emit('save', {
      username: username.value.trim(),
      displayName: displayName.value.trim(),
      password: password.value.trim(),
      role: role.value
    });
  } else {
    emit('save', {
      username: username.value.trim(),
      displayName: displayName.value.trim(),
      password: password.value.trim() || null,
      role: role.value,
      isActive: isActive.value
    });
  }
}
</script>

<template>
  <v-dialog v-model="visible" :persistent="props.busy">
    <v-card>
      <v-card-title class="text-h6">
        {{ props.mode === 'create' ? 'Создание пользователя' : 'Редактирование пользователя' }}
      </v-card-title>

      <v-card-text class="d-flex flex-column ga-4">
        <v-text-field
          v-model="username"
          label="Логин"
          maxlength="50"
          counter
          :disabled="props.busy"
        ></v-text-field>

        <v-text-field
          v-model="displayName"
          label="Отображаемое имя"
          maxlength="100"
          counter
          :disabled="props.busy"
        ></v-text-field>

        <v-text-field
          v-model="password"
          :label="props.mode === 'create' ? 'Пароль' : 'Новый пароль (необязательно)'"
          :type="props.mode === 'create' ? 'password' : 'text'"
          :disabled="props.busy"
        ></v-text-field>

        <v-select
          v-model="role"
          label="Роль"
          :items="roleItems"
          item-title="title"
          item-value="value"
          :disabled="props.busy"
        ></v-select>

        <v-switch
          v-if="props.mode === 'edit'"
          v-model="isActive"
          color="success"
          label="Учетная запись активна"
          inset
          :disabled="props.busy"
        ></v-switch>

        <v-alert v-if="validationMessage" type="warning" variant="tonal">
          {{ validationMessage }}
        </v-alert>
      </v-card-text>

      <v-card-actions class="justify-end">
        <v-btn variant="text" :disabled="props.busy" @click="visible = false">Отмена</v-btn>
        <v-btn
          color="primary"
          variant="flat"
          :disabled="!canSave"
          :loading="props.busy"
          @click="handleSave"
        >
          Сохранить
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
