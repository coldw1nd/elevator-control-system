<script setup lang="ts">
import { onMounted, ref } from 'vue';
import GlobalSnackbar from '@/components/common/GlobalSnackbar.vue';
import { authService } from '@/services/auth-service';
import { clearAuthSession } from '@/services/auth-storage';
import type { CurrentUserDto } from '@/types/api';
import { useSnackbar } from '@/composables/useSnackbar';
import { extractErrorMessage } from '@/utils/errors';
import { resolvePostLoginUrl } from '@/utils/urls';

const username = ref('admin');
const password = ref('Admin123!');
const loading = ref(false);
const checkingSession = ref(true);
const formError = ref('');

const { snackbar, showError } = useSnackbar();

function redirectAfterLogin(user: CurrentUserDto): void {
  const targetUrl = resolvePostLoginUrl(user);

  window.location.href = targetUrl;
}

async function login(): Promise<void> {
  formError.value = '';

  if (username.value.trim().length < 3) {
    formError.value = 'Введите корректное имя пользователя.';
    return;
  }

  if (password.value.trim().length < 8) {
    formError.value = 'Пароль должен содержать не менее 8 символов.';
    return;
  }

  loading.value = true;

  try {
    const result = await authService.login({
      username: username.value.trim(),
      password: password.value
    });

    redirectAfterLogin(result.user);
  } catch (error) {
    formError.value = extractErrorMessage(error);
    showError(formError.value);
  } finally {
    loading.value = false;
  }
}

onMounted(async () => {
  if (!authService.isAuthenticated()) {
    checkingSession.value = false;
    return;
  }

  try {
    const user = await authService.getCurrentUser();

    redirectAfterLogin(user);
  } catch {
    clearAuthSession();
    checkingSession.value = false;
  }
});
</script>

<template>
  <v-app>
    <v-main class="login-page__main">
      <div class="login-page">
        <v-container class="login-page__container">
          <div class="login-page__surface">
            <v-row class="login-page__row" align="center">
              <v-col cols="12" lg="6" class="mb-10 mb-lg-0">
                <div class="login-page__intro">
                  <div class="login-page__badge">
                    MPA • Vue 3 • Vuetify • REST • SignalR
                  </div>

                  <h1 class="login-page__title">
                    Elevator Control System
                  </h1>

                  <p class="login-page__subtitle">
                    Веб-приложение для имитации автоматизированной системы контроля работы лифта,
                    управления сеансами, мониторинга состояния кабины, формирования итоговых отчётов
                    и ведения журнала аудита.
                  </p>

                  <div class="login-page__features">
                    <div class="login-page__feature">
                      <v-icon color="primary">mdi-elevator-passenger</v-icon>
                      <span>Плавная визуализация движения лифта</span>
                    </div>

                    <div class="login-page__feature">
                      <v-icon color="success">mdi-account-group</v-icon>
                      <span>Создание пассажиров в режиме реального времени</span>
                    </div>

                    <div class="login-page__feature">
                      <v-icon color="warning">mdi-chart-box-outline</v-icon>
                      <span>Экспорт итоговой статистики в Excel</span>
                    </div>

                    <div class="login-page__feature">
                      <v-icon color="secondary">mdi-shield-account</v-icon>
                      <span>Роли пользователей и аудит действий</span>
                    </div>
                  </div>
                </div>
              </v-col>

              <v-col cols="12" lg="6" class="d-flex justify-center">
                <div class="login-page__form-wrap">
                  <v-card class="login-page__card pa-4 pa-sm-6" elevation="0">
                    <v-card-title class="text-h5 font-weight-bold">
                      Вход в систему
                    </v-card-title>

                    <v-card-subtitle class="mt-2">
                      Авторизуйтесь для работы с сеансами, мониторингом и отчётами.
                    </v-card-subtitle>

                    <v-card-text class="pt-6">
                      <v-alert
                        v-if="checkingSession"
                        type="info"
                        variant="tonal"
                        class="mb-4"
                      >
                        Проверка текущей сессии...
                      </v-alert>

                      <v-alert
                        v-if="formError"
                        type="error"
                        variant="tonal"
                        class="mb-4"
                      >
                        {{ formError }}
                      </v-alert>

                      <div class="login-page__form-fields">
                        <v-text-field
                          v-model="username"
                          label="Имя пользователя"
                          prepend-inner-icon="mdi-account-outline"
                          autocomplete="username"
                        ></v-text-field>

                        <v-text-field
                          v-model="password"
                          label="Пароль"
                          prepend-inner-icon="mdi-lock-outline"
                          autocomplete="current-password"
                          type="password"
                          @keyup.enter="login"
                        ></v-text-field>
                      </div>

                      <v-btn
                        class="mt-4"
                        color="primary"
                        variant="flat"
                        block
                        size="large"
                        prepend-icon="mdi-login"
                        :loading="loading"
                        :disabled="checkingSession"
                        @click="login"
                      >
                        Войти
                      </v-btn>

                      <v-divider class="my-6"></v-divider>

                      <div class="text-body-2 text-medium-emphasis mb-3">
                        Тестовые учетные записи:
                      </div>

                      <div class="d-flex flex-column ga-3">
                        <v-sheet class="login-page__demo-sheet">
                          <div class="font-weight-bold">Администратор</div>
                          <div>Логин: <b>admin</b></div>
                          <div>Пароль: <b>Admin123!</b></div>
                        </v-sheet>

                        <v-sheet class="login-page__demo-sheet">
                          <div class="font-weight-bold">Оператор</div>
                          <div>Логин: <b>operator</b></div>
                          <div>Пароль: <b>Operator123!</b></div>
                        </v-sheet>

                        <v-sheet class="login-page__demo-sheet">
                          <div class="font-weight-bold">Наблюдатель</div>
                          <div>Логин: <b>viewer</b></div>
                          <div>Пароль: <b>Viewer123!</b></div>
                        </v-sheet>
                      </div>
                    </v-card-text>
                  </v-card>
                </div>
              </v-col>
            </v-row>
          </div>
        </v-container>
      </div>
    </v-main>

    <GlobalSnackbar
      v-model="snackbar.visible"
      :text="snackbar.text"
      :color="snackbar.color"
      :timeout="snackbar.timeout"
    />
  </v-app>
</template>

<style scoped>
.login-page__main {
  display: flex;
}

.login-page {
  min-height: 100vh;
  width: 100%;
  display: flex;
  align-items: center;
  background:
    radial-gradient(circle at top left, rgba(37, 99, 235, 0.16), transparent 28%),
    radial-gradient(circle at bottom right, rgba(20, 184, 166, 0.14), transparent 24%),
    linear-gradient(180deg, #f8fafc 0%, #eff6ff 100%);
}

.login-page__container {
  width: 100%;
  max-width: 1480px !important;
  padding: clamp(24px, 4vw, 48px) !important;
}

.login-page__surface {
  position: relative;
  width: 100%;
  overflow: hidden;
  border-radius: 36px;
  background: rgba(255, 255, 255, 0.82);
  border: 1px solid rgba(148, 163, 184, 0.18);
  box-shadow: 0 36px 80px rgba(15, 23, 42, 0.14);
  backdrop-filter: blur(14px);
  padding: clamp(24px, 3vw, 40px);
}

.login-page__surface::before,
.login-page__surface::after {
  content: '';
  position: absolute;
  pointer-events: none;
  border-radius: 999px;
  filter: blur(6px);
}

.login-page__surface::before {
  top: -180px;
  left: -120px;
  width: 420px;
  height: 420px;
  background: radial-gradient(circle, rgba(59, 130, 246, 0.22) 0%, transparent 70%);
}

.login-page__surface::after {
  right: -140px;
  bottom: -200px;
  width: 460px;
  height: 460px;
  background: radial-gradient(circle, rgba(20, 184, 166, 0.18) 0%, transparent 72%);
}

.login-page__row {
  position: relative;
  z-index: 1;
  min-height: min(780px, calc(100vh - 120px));
}

.login-page__intro {
  max-width: 620px;
  padding-left: clamp(8px, 1vw, 16px);
}

.login-page__badge {
  display: inline-flex;
  padding: 10px 16px;
  border-radius: 999px;
  background: rgba(37, 99, 235, 0.1);
  color: #1d4ed8;
  font-weight: 700;
  margin-bottom: 20px;
  box-shadow: inset 0 0 0 1px rgba(37, 99, 235, 0.08);
}

.login-page__title {
  font-size: clamp(2.4rem, 4vw, 3.6rem);
  line-height: 1.02;
  font-weight: 800;
  color: #0f172a;
}

.login-page__subtitle {
  margin-top: 20px;
  font-size: 1.05rem;
  line-height: 1.75;
  color: #475569;
}

.login-page__features {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin-top: 28px;
}

.login-page__feature {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 1rem;
  color: #0f172a;
}

.login-page__form-wrap {
  width: 100%;
  min-height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.login-page__card {
  width: min(100%, 540px);
  border: 1px solid rgba(148, 163, 184, 0.16);
  background: rgba(255, 255, 255, 0.94);
  box-shadow: 0 24px 60px rgba(15, 23, 42, 0.12);
  backdrop-filter: blur(10px);
}

.login-page__form-fields {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.login-page__demo-sheet {
  padding: 14px 16px;
  border-radius: 16px;
  background: rgba(248, 250, 252, 0.92);
  border: 1px solid rgba(148, 163, 184, 0.18);
}

@media (max-width: 1264px) {
  .login-page__row {
    min-height: auto;
  }
}

@media (max-width: 960px) {
  .login-page__container {
    padding: 20px !important;
  }

  .login-page__surface {
    padding: 22px;
    border-radius: 28px;
  }

  .login-page__intro {
    padding-left: 0;
  }

  .login-page__title {
    font-size: clamp(2rem, 10vw, 2.8rem);
  }
}
</style>
