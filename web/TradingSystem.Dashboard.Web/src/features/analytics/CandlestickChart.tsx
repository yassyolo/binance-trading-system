import type { ChartMarkerDto, PriceCandleDto } from '@/types/analytics'

interface CandlestickChartProps {
  candles: PriceCandleDto[]
  markers: ChartMarkerDto[]
}

const width = 1200
const height = 430
const padding = {
  top: 22,
  right: 76,
  bottom: 40,
  left: 16,
}

export function CandlestickChart({
  candles,
  markers,
}: CandlestickChartProps) {
  if (candles.length === 0)
    return null

  const displayed = candles.slice(-180)
  const firstTime = new Date(displayed[0].openTimeUtc).getTime()
  const lastTime = new Date(displayed[displayed.length - 1].openTimeUtc).getTime()

  const minPrice = Math.min(...displayed.map((item) => item.low))
  const maxPrice = Math.max(...displayed.map((item) => item.high))
  const rawRange = Math.max(maxPrice - minPrice, 0.00000001)
  const pricePadding = rawRange * 0.08
  const chartMin = minPrice - pricePadding
  const chartMax = maxPrice + pricePadding
  const chartRange = chartMax - chartMin

  const innerWidth = width - padding.left - padding.right
  const innerHeight = height - padding.top - padding.bottom
  const step = innerWidth / Math.max(displayed.length, 1)
  const bodyWidth = Math.max(2, Math.min(8, step * 0.56))

  function y(price: number) {
    return (
      padding.top +
      ((chartMax - price) / chartRange) * innerHeight
    )
  }

  function x(index: number) {
    return padding.left + step * index + step / 2
  }

  const visibleMarkers = markers.filter((marker) => {
    const time = new Date(marker.timeUtc).getTime()
    return time >= firstTime && time <= lastTime
  })

  function markerX(marker: ChartMarkerDto) {
    const markerTime = new Date(marker.timeUtc).getTime()
    const span = Math.max(lastTime - firstTime, 1)
    return padding.left + ((markerTime - firstTime) / span) * innerWidth
  }

  const priceTicks = Array.from({ length: 6 }, (_, index) => {
    const ratio = index / 5
    const price = chartMax - chartRange * ratio
    return { price, y: padding.top + innerHeight * ratio }
  })

  const timeTicks = Array.from({ length: 6 }, (_, index) => {
    const candleIndex = Math.min(
      displayed.length - 1,
      Math.round((displayed.length - 1) * (index / 5)),
    )

    return {
      x: x(candleIndex),
      time: displayed[candleIndex].openTimeUtc,
    }
  })

  return (
    <div className="overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-app)]">
      <svg
        viewBox={`0 0 ${width} ${height}`}
        className="block h-auto w-full"
        role="img"
        aria-label="Candlestick price chart"
      >
        {priceTicks.map((tick) => (
          <g key={tick.price}>
            <line
              x1={padding.left}
              x2={width - padding.right}
              y1={tick.y}
              y2={tick.y}
              stroke="var(--color-border)"
              strokeDasharray="3 4"
            />
            <text
              x={width - padding.right + 10}
              y={tick.y + 4}
              fill="var(--color-text-muted)"
              fontSize="11"
            >
              {formatPrice(tick.price)}
            </text>
          </g>
        ))}

        {timeTicks.map((tick) => (
          <text
            key={`${tick.x}-${tick.time}`}
            x={tick.x}
            y={height - 14}
            fill="var(--color-text-muted)"
            fontSize="10"
            textAnchor="middle"
          >
            {formatTime(tick.time)}
          </text>
        ))}

        {displayed.map((candle, index) => {
          const candleX = x(index)
          const openY = y(candle.open)
          const closeY = y(candle.close)
          const highY = y(candle.high)
          const lowY = y(candle.low)
          const positive = candle.close >= candle.open
          const candleColor = positive
            ? 'var(--color-success)'
            : 'var(--color-danger)'
          const bodyY = Math.min(openY, closeY)
          const bodyHeight = Math.max(1.5, Math.abs(closeY - openY))

          return (
            <g key={`${candle.openTimeUtc}-${index}`}>
              <line
                x1={candleX}
                x2={candleX}
                y1={highY}
                y2={lowY}
                stroke={candleColor}
                strokeWidth="1"
              />
              <rect
                x={candleX - bodyWidth / 2}
                y={bodyY}
                width={bodyWidth}
                height={bodyHeight}
                rx="0.6"
                fill={candleColor}
              >
                <title>
                  {`${formatTime(candle.openTimeUtc)}  O ${formatPrice(candle.open)}  H ${formatPrice(candle.high)}  L ${formatPrice(candle.low)}  C ${formatPrice(candle.close)}`}
                </title>
              </rect>
            </g>
          )
        })}

        {visibleMarkers.map((marker, index) => {
          const cx = markerX(marker)
          const cy = y(marker.price)
          const longSide =
            marker.side.toLowerCase().includes('long') ||
            marker.side.toLowerCase().includes('buy')

          return (
            <g key={`${marker.timeUtc}-${marker.kind}-${index}`}>
              <circle
                cx={cx}
                cy={cy}
                r="5"
                fill={
                  longSide
                    ? 'var(--color-success)'
                    : 'var(--color-danger)'
                }
                stroke="var(--color-app)"
                strokeWidth="2"
              >
                <title>
                  {`${marker.kind} · ${marker.side} · ${formatPrice(marker.price)}${marker.label ? ` · ${marker.label}` : ''}`}
                </title>
              </circle>
            </g>
          )
        })}
      </svg>
    </div>
  )
}

function formatPrice(value: number) {
  return new Intl.NumberFormat('en-US', {
    maximumFractionDigits: 4,
  }).format(value)
}

function formatTime(value: string) {
  const date = new Date(value)

  if (Number.isNaN(date.getTime()))
    return value

  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
    timeZone: 'UTC',
  }).format(date)
}
