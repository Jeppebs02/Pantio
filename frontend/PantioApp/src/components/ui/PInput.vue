<script setup lang="ts">
defineProps<{
  label?: string
  placeholder?: string
  error?: string
  type?: string
  modelValue?: string | number
}>()

defineEmits<{
  'update:modelValue': [value: string]
}>()
</script>

<template>
  <div class="pinput-wrap">
    <label v-if="label" class="pinput-label eyebrow">{{ label }}</label>
    <div class="pinput-field" :class="{ 'pinput-field--error': error }">
      <span v-if="$slots.icon" class="pinput-icon">
        <slot name="icon" />
      </span>
      <input
        :type="type ?? 'text'"
        :placeholder="placeholder"
        :value="modelValue"
        class="pinput"
        :class="{ 'pinput--has-icon': $slots.icon }"
        @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
      />
    </div>
    <p v-if="error" class="pinput-error">{{ error }}</p>
  </div>
</template>

<style scoped>
.pinput-wrap {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.pinput-label {
  display: block;
}

.pinput-field {
  position: relative;
  display: flex;
  align-items: center;
}

.pinput-icon {
  position: absolute;
  left: 12px;
  color: var(--fg-faint);
  display: flex;
  align-items: center;
  pointer-events: none;
}

.pinput {
  width: 100%;
  padding: 10px 12px;
  border-radius: var(--radius-md);
  border: 1px solid var(--border);
  background: var(--surface);
  color: var(--fg);
  font-size: 15px;
  line-height: 24px;
  box-shadow: var(--shadow-sm);
  transition: border-color var(--motion-default), box-shadow var(--motion-default);
  outline: none;
}

.pinput--has-icon {
  padding-left: 38px;
}

.pinput::placeholder {
  color: var(--fg-faint);
}

.pinput:focus {
  border-color: var(--sage-600);
  box-shadow: 0 0 0 3px var(--sage-100);
}

.pinput-field--error .pinput {
  border-color: var(--past);
  box-shadow: 0 0 0 3px var(--past-bg);
}

.pinput-error {
  font-size: 13px;
  color: var(--past);
}
</style>
