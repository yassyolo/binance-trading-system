import { useEffect, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { RotateCcw, Save } from 'lucide-react'

import { ApiError } from '@/api/api-error'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ErrorState } from '@/components/ui/ErrorState'
import {
  environmentValue,
  tradingEnvironmentValues,
} from '@/features/bots/bot-enums'
import { useUpdateBotConfiguration } from '@/features/bots/bots.queries'
import type { BotConfigurationDto } from '@/types/bots'

interface BotConfigurationFormProps {
  configuration: BotConfigurationDto
}

interface FormState {
  strategyType: string
  symbol: string
  environment: number
  signalSource: string
  enableLong: boolean
  enableShort: boolean
  quantity: string
  leverage: string
  priceDistance: string
  profitDistance: string
  orderSideLimit: string
  cooldownSeconds: string
  reason: string
}

function fromConfiguration(configuration: BotConfigurationDto): FormState {
  return {
    strategyType: configuration.strategyType,
    symbol: configuration.symbol,
    environment: environmentValue(configuration.environment),
    signalSource: configuration.signalSource,
    enableLong: configuration.enableLong,
    enableShort: configuration.enableShort,
    quantity: String(configuration.quantity),
    leverage: String(configuration.leverage),
    priceDistance: configuration.priceDistance?.toString() ?? '',
    profitDistance: configuration.profitDistance?.toString() ?? '',
    orderSideLimit: configuration.orderSideLimit?.toString() ?? '',
    cooldownSeconds: String(configuration.cooldownSeconds),
    reason: '',
  }
}

function optionalNumber(value: string) {
  const normalized = value.trim()
  return normalized === '' ? null : Number(normalized)
}

export function BotConfigurationForm({
  configuration,
}: BotConfigurationFormProps) {
  const [form, setForm] = useState(() => fromConfiguration(configuration))
  const mutation = useUpdateBotConfiguration(configuration.botName)

  useEffect(() => {
    setForm(fromConfiguration(configuration))
  }, [configuration])

  const error = mutation.error instanceof ApiError ? mutation.error : null

  function update<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [key]: value }))
  }

  function reset() {
    setForm(fromConfiguration(configuration))
    mutation.reset()
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    mutation.mutate({
      expectedVersion: configuration.version,
      strategyType: form.strategyType.trim(),
      symbol: form.symbol.trim().toUpperCase(),
      environment: form.environment,
      signalSource: form.signalSource.trim(),
      enableLong: form.enableLong,
      enableShort: form.enableShort,
      quantity: Number(form.quantity),
      leverage: Number(form.leverage),
      priceDistance: optionalNumber(form.priceDistance),
      profitDistance: optionalNumber(form.profitDistance),
      orderSideLimit: optionalNumber(form.orderSideLimit),
      cooldownSeconds: Number(form.cooldownSeconds),
      reason: form.reason.trim(),
    })
  }

  return (
    <form onSubmit={submit} className="space-y-5">
      {error && (
        <ErrorState
          title={error.status === 409 ? 'Configuration version conflict' : 'Configuration update failed'}
          description={error.message}
        />
      )}

      {mutation.isSuccess && (
        <Card className="border-[rgba(74,222,128,0.18)] bg-[rgba(74,222,128,0.04)] p-4 text-sm text-[var(--color-success)]">
          Configuration saved successfully.
          {mutation.data.restartRequired && ' Restart required before every change is active.'}
        </Card>
      )}

      <Card className="p-6">
        <SectionTitle
          title="Identity & environment"
          description="Core runtime identity and execution environment."
        />

        <div className="mt-5 grid grid-cols-3 gap-4">
          <Field label="Strategy type">
            <input
              className={inputClass}
              value={form.strategyType}
              onChange={(event) => update('strategyType', event.target.value)}
              required
            />
          </Field>

          <Field label="Symbol">
            <input
              className={inputClass}
              value={form.symbol}
              onChange={(event) => update('symbol', event.target.value)}
              required
            />
          </Field>

          <Field label="Environment">
            <select
              className={inputClass}
              value={form.environment}
              onChange={(event) => update('environment', Number(event.target.value))}
            >
              <option value={tradingEnvironmentValues.Demo}>Demo</option>
              <option value={tradingEnvironmentValues.Production}>Production</option>
            </select>
          </Field>

          <Field label="Signal source">
            <input
              className={inputClass}
              value={form.signalSource}
              onChange={(event) => update('signalSource', event.target.value)}
              required
            />
          </Field>
        </div>
      </Card>

      <Card className="p-6">
        <SectionTitle
          title="Execution"
          description="Quantity, leverage and permitted trade directions."
        />

        <div className="mt-5 grid grid-cols-3 gap-4">
          <Field label="Quantity">
            <input
              className={inputClass}
              type="number"
              min="0"
              step="any"
              value={form.quantity}
              onChange={(event) => update('quantity', event.target.value)}
              required
            />
          </Field>

          <Field label="Leverage">
            <input
              className={inputClass}
              type="number"
              min="1"
              max="125"
              value={form.leverage}
              onChange={(event) => update('leverage', event.target.value)}
              required
            />
          </Field>

          <Field label="Cooldown (seconds)">
            <input
              className={inputClass}
              type="number"
              min="0"
              value={form.cooldownSeconds}
              onChange={(event) => update('cooldownSeconds', event.target.value)}
              required
            />
          </Field>
        </div>

        <div className="mt-6 flex gap-3">
          <ToggleCard
            label="Enable Long"
            description="Allow the strategy to open long positions."
            checked={form.enableLong}
            onChange={(value) => update('enableLong', value)}
          />

          <ToggleCard
            label="Enable Short"
            description="Allow the strategy to open short positions."
            checked={form.enableShort}
            onChange={(value) => update('enableShort', value)}
          />
        </div>
      </Card>

      <Card className="p-6">
        <SectionTitle
          title="Strategy parameters"
          description="Nullable parameters remain empty when they are not used by the selected strategy."
        />

        <div className="mt-5 grid grid-cols-3 gap-4">
          <Field label="Price distance">
            <input
              className={inputClass}
              type="number"
              min="0"
              step="any"
              value={form.priceDistance}
              onChange={(event) => update('priceDistance', event.target.value)}
              placeholder="Not configured"
            />
          </Field>

          <Field label="Profit distance">
            <input
              className={inputClass}
              type="number"
              min="0"
              step="any"
              value={form.profitDistance}
              onChange={(event) => update('profitDistance', event.target.value)}
              placeholder="Not configured"
            />
          </Field>

          <Field label="Order side limit">
            <input
              className={inputClass}
              type="number"
              min="1"
              value={form.orderSideLimit}
              onChange={(event) => update('orderSideLimit', event.target.value)}
              placeholder="Not configured"
            />
          </Field>
        </div>
      </Card>

      <Card className="p-6">
        <SectionTitle
          title="Change reason"
          description="Required by the backend and stored in the audit trail."
        />

        <textarea
          className={`${inputClass} mt-5 min-h-24 resize-y`}
          value={form.reason}
          onChange={(event) => update('reason', event.target.value)}
          placeholder="Explain why this configuration is being changed..."
          required
        />

        <div className="mt-5 flex justify-end gap-2">
          <Button
            leftIcon={<RotateCcw size={15} />}
            onClick={reset}
            disabled={mutation.isPending}
          >
            Reset
          </Button>
          <Button
            type="submit"
            variant="primary"
            leftIcon={<Save size={15} />}
            disabled={mutation.isPending || form.reason.trim() === ''}
          >
            {mutation.isPending ? 'Saving…' : 'Save configuration'}
          </Button>
        </div>
      </Card>
    </form>
  )
}

const inputClass =
  'h-10 w-full rounded-xl border border-[var(--color-border)] bg-[var(--color-app)] px-3 text-sm text-[var(--color-text-primary)] outline-none transition placeholder:text-[var(--color-text-muted)] focus:border-[#454954]'

function Field({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <label>
      <span className="mb-2 block text-xs font-medium text-[var(--color-text-secondary)]">
        {label}
      </span>
      {children}
    </label>
  )
}

function SectionTitle({
  title,
  description,
}: {
  title: string
  description: string
}) {
  return (
    <div>
      <h3 className="text-sm font-semibold">{title}</h3>
      <p className="mt-1 text-xs leading-5 text-[var(--color-text-muted)]">
        {description}
      </p>
    </div>
  )
}

function ToggleCard({
  label,
  description,
  checked,
  onChange,
}: {
  label: string
  description: string
  checked: boolean
  onChange: (checked: boolean) => void
}) {
  return (
    <label className="flex min-w-64 cursor-pointer items-center justify-between gap-6 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-strong)] p-4">
      <div>
        <p className="text-sm font-medium">{label}</p>
        <p className="mt-1 text-xs text-[var(--color-text-muted)]">{description}</p>
      </div>

      <input
        type="checkbox"
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
        className="size-4 accent-white"
      />
    </label>
  )
}
