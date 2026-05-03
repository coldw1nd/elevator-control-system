<script setup lang="ts">
import { computed } from 'vue';
import type { CurrentUserDto, NavItem, StatusBarDto } from '@/types/api';
import { formatRole } from '@/utils/format';

const props = defineProps<{
  pageTitle: string;
  activePage: string;
  currentUser: CurrentUserDto | null;
  statusBar?: StatusBarDto | null;
}>();

defineEmits<{
  logout: [];
}>();

const navigationItems = computed<NavItem[]>(() => {
  const items: NavItem[] = [
    {
      key: 'sessions',
      title: 'Сеансы',
      href: '/sessions/',
      roles: ['Admin', 'Operator', 'Viewer'],
      icon: 'mdi-format-list-bulleted-square'
    },
    {
      key: 'dashboard',
      title: 'Мониторинг',
      href: '/dashboard/',
      roles: ['Admin', 'Operator', 'Viewer'],
      icon: 'mdi-monitor-dashboard'
    },
    {
      key: 'reports',
      title: 'Отчёты',
      href: '/reports/',
      roles: ['Admin', 'Operator', 'Viewer'],
      icon: 'mdi-file-chart'
    },
    {
      key: 'admin',
      title: 'Пользователи',
      href: '/admin/',
      roles: ['Admin'],
      icon: 'mdi-account-cog'
    },
    {
      key: 'audit',
      title: 'Аудит',
      href: '/audit/',
      roles: ['Admin'],
      icon: 'mdi-history'
    }
  ];

  if (!props.currentUser) {
    return [];
  }

  return items.filter((item) => item.roles.includes(props.currentUser!.role));
});

const initials = computed(() => {
  const displayName = props.currentUser?.displayName ?? '';
  const parts = displayName.split(' ').filter(Boolean);

  if (parts.length === 0) {
    return 'U';
  }

  return parts
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('');
});

const movingElevatorsText = computed(() => {
  return props.statusBar ? String(props.statusBar.movingElevatorsCount) : '—';
});

const stoppedElevatorsText = computed(() => {
  return props.statusBar ? String(props.statusBar.stoppedElevatorsCount) : '—';
});

const transportedPassengersText = computed(() => {
  return props.statusBar ? String(props.statusBar.transportedPassengersCount) : '—';
});
</script>

<template>
  <v-app>
    <v-layout class="app-shell">
      <v-app-bar flat height="78" class="app-shell__bar">
        <div class="app-shell__brand">
          <div class="app-shell__logo">
            <div class="app-shell__logo-door app-shell__logo-door--left"></div>
            <div class="app-shell__logo-door app-shell__logo-door--right"></div>
          </div>

          <div>
            <div class="text-subtitle-1 font-weight-bold">Elevator Control System</div>
            <div class="text-caption text-medium-emphasis">
              Автоматизированная система контроля работы лифта
            </div>
          </div>
        </div>

        <v-divider vertical class="mx-4 d-none d-lg-flex" />

        <div class="d-none d-md-flex flex-wrap ga-2">
          <v-btn
            v-for="item in navigationItems"
            :key="item.key"
            :href="item.href"
            :prepend-icon="item.icon"
            :variant="item.key === activePage ? 'flat' : 'text'"
            :color="item.key === activePage ? 'primary' : undefined"
          >
            {{ item.title }}
          </v-btn>
        </div>

        <v-spacer />

        <div class="d-md-none">
          <v-menu>
            <template #activator="{ props: menuProps }">
              <v-btn icon="mdi-menu" v-bind="menuProps" variant="text"></v-btn>
            </template>

            <v-list min-width="240">
              <v-list-item
                v-for="item in navigationItems"
                :key="item.key"
                :href="item.href"
                :prepend-icon="item.icon"
                :title="item.title"
              ></v-list-item>
            </v-list>
          </v-menu>
        </div>

        <div
          v-if="currentUser"
          class="app-shell__user-block d-flex align-center ga-3 ml-4"
        >
          <div class="text-right d-none d-lg-block">
            <div class="text-body-2 font-weight-medium">{{ currentUser.displayName }}</div>
            <div class="text-caption text-medium-emphasis">{{ formatRole(currentUser.role) }}</div>
          </div>

          <v-avatar color="primary" size="40">
            <span class="text-white font-weight-bold">{{ initials }}</span>
          </v-avatar>

          <v-btn
            variant="tonal"
            color="secondary"
            prepend-icon="mdi-logout"
            @click="$emit('logout')"
          >
            Выйти
          </v-btn>
        </div>
      </v-app-bar>

      <v-main class="app-shell__main">
        <v-container fluid class="app-shell__container py-6">
          <div class="app-shell__page-header d-flex align-start justify-space-between flex-wrap ga-4 mb-6">
            <div class="app-shell__page-title-block">
              <div class="text-overline text-primary font-weight-bold">
                Информационная система контроля и имитации работы лифта
              </div>
              <h1 class="text-h4 font-weight-bold mt-1">{{ pageTitle }}</h1>
            </div>

            <div class="d-flex align-center ga-3 flex-wrap app-shell__page-toolbar">
              <v-chip
                v-if="currentUser"
                color="secondary"
                variant="tonal"
                prepend-icon="mdi-account-badge"
              >
                {{ formatRole(currentUser.role) }}
              </v-chip>

              <slot name="toolbar" />
            </div>
          </div>

          <div class="app-shell__status-grid mb-6">
            <div class="app-shell__status-card">
              <div class="app-shell__status-icon-wrap app-shell__status-icon-wrap--success">
                <v-icon color="success">mdi-arrow-up-bold-circle</v-icon>
              </div>

              <div class="app-shell__status-content">
                <div class="text-body-2 text-medium-emphasis">Лифтов в движении</div>
                <div class="app-shell__status-value">{{ movingElevatorsText }}</div>
              </div>
            </div>

            <div class="app-shell__status-card">
              <div class="app-shell__status-icon-wrap app-shell__status-icon-wrap--secondary">
                <v-icon color="secondary">mdi-pause-circle</v-icon>
              </div>

              <div class="app-shell__status-content">
                <div class="text-body-2 text-medium-emphasis">Лифтов остановлено</div>
                <div class="app-shell__status-value">{{ stoppedElevatorsText }}</div>
              </div>
            </div>

            <div class="app-shell__status-card">
              <div class="app-shell__status-icon-wrap app-shell__status-icon-wrap--primary">
                <v-icon color="primary">mdi-account-group</v-icon>
              </div>

              <div class="app-shell__status-content">
                <div class="text-body-2 text-medium-emphasis">Перевезено пассажиров</div>
                <div class="app-shell__status-value">{{ transportedPassengersText }}</div>
              </div>
            </div>
          </div>

          <slot />
        </v-container>
      </v-main>
    </v-layout>
  </v-app>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
}

.app-shell__bar {
  border-bottom: 1px solid rgba(15, 23, 42, 0.08);
  background: rgba(255, 255, 255, 0.92);
  backdrop-filter: blur(12px);
}

.app-shell__bar :deep(.v-toolbar__content) {
  padding-inline: clamp(20px, 3vw, 32px);
  gap: 16px;
}

.app-shell__brand {
  display: flex;
  align-items: center;
  gap: 16px;
}

.app-shell__logo {
  position: relative;
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: linear-gradient(180deg, #0f172a 0%, #1e293b 100%);
  overflow: hidden;
  box-shadow: 0 10px 24px rgba(37, 99, 235, 0.24);
}

.app-shell__logo-door {
  position: absolute;
  top: 8px;
  bottom: 8px;
  width: 12px;
  border-radius: 4px;
}

.app-shell__logo-door--left {
  left: 8px;
  background: linear-gradient(180deg, #93c5fd 0%, #60a5fa 100%);
}

.app-shell__logo-door--right {
  right: 8px;
  background: linear-gradient(180deg, #7dd3fc 0%, #38bdf8 100%);
}

.app-shell__user-block {
  min-width: 0;
  padding-right: 8px;
}

.app-shell__main {
  background: transparent;
}

.app-shell__container {
  width: 100%;
  max-width: 1720px;
  margin: 0 auto;
  padding-inline: clamp(24px, 3.4vw, 44px) !important;
}

.app-shell__page-header {
  padding-inline: 4px;
}

.app-shell__page-title-block {
  min-width: 0;
  max-width: min(900px, 100%);
  padding-left: 2px;
}

.app-shell__page-toolbar {
  padding-right: 2px;
}

.app-shell__status-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 12px;
  padding-inline: 4px;
}

.app-shell__status-card {
  display: flex;
  align-items: center;
  gap: 14px;
  min-width: 0;
  padding: 14px 16px;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid rgba(148, 163, 184, 0.22);
  box-shadow: 0 10px 24px rgba(15, 23, 42, 0.05);
}

.app-shell__status-icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 48px;
  border-radius: 14px;
  flex: 0 0 auto;
}

.app-shell__status-icon-wrap--success {
  background: rgba(22, 163, 74, 0.10);
}

.app-shell__status-icon-wrap--secondary {
  background: rgba(15, 23, 42, 0.08);
}

.app-shell__status-icon-wrap--primary {
  background: rgba(37, 99, 235, 0.10);
}

.app-shell__status-content {
  min-width: 0;
}

.app-shell__status-value {
  margin-top: 4px;
  font-size: 1.35rem;
  line-height: 1;
  font-weight: 700;
  color: #0f172a;
}

@media (max-width: 960px) {
  .app-shell__bar :deep(.v-toolbar__content) {
    padding-inline: 16px;
  }

  .app-shell__container {
    padding-inline: 18px !important;
  }

  .app-shell__page-header {
    padding-inline: 0;
  }

  .app-shell__status-grid {
    padding-inline: 0;
  }

  .app-shell__user-block {
    padding-right: 0;
  }
}
</style>
