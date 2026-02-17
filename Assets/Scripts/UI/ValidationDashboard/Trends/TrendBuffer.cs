// ============================================================================
// CRITICAL: Master the Atom - Trend Buffer
// TrendBuffer.cs - Ring Buffer for Time-Series Data
// ============================================================================

using UnityEngine;

namespace Critical.UI.ValidationDashboard
{
    public class TrendBuffer
    {
        private readonly float[] _values;
        private readonly float[] _times;
        private readonly int _capacity;
        private int _head;
        private int _count;
        private float _minValue;
        private float _maxValue;
        private bool _autoScale;
        private float _fixedMin;
        private float _fixedMax;

        public int Capacity => _capacity;
        public int Count => _count;
        public float MinValue => _autoScale ? _minValue : _fixedMin;
        public float MaxValue => _autoScale ? _maxValue : _fixedMax;
        public bool IsEmpty => _count == 0;

        public TrendBuffer(int capacity, float fixedMin = float.NaN, float fixedMax = float.NaN)
        {
            _capacity = capacity;
            _values = new float[capacity];
            _times = new float[capacity];
            _head = 0;
            _count = 0;
            _minValue = float.MaxValue;
            _maxValue = float.MinValue;
            _autoScale = float.IsNaN(fixedMin) || float.IsNaN(fixedMax);
            _fixedMin = float.IsNaN(fixedMin) ? 0f : fixedMin;
            _fixedMax = float.IsNaN(fixedMax) ? 100f : fixedMax;
        }

        public void Add(float time, float value)
        {
            _values[_head] = value;
            _times[_head] = time;
            _head = (_head + 1) % _capacity;
            if (_count < _capacity) _count++;
            if (_autoScale) RecalculateMinMax();
        }

        public float GetValue(int index)
        {
            if (index < 0 || index >= _count) return 0f;
            return _values[GetIndex(index)];
        }

        public float GetNormalizedValue(int index)
        {
            float value = GetValue(index);
            float range = MaxValue - MinValue;
            if (range <= 0f) return 0.5f;
            return Mathf.Clamp01((value - MinValue) / range);
        }

        public float GetLatest() => _count == 0 ? 0f : _values[GetIndex(_count - 1)];

        public void Clear()
        {
            _head = 0;
            _count = 0;
            _minValue = float.MaxValue;
            _maxValue = float.MinValue;
        }

        public void SetRange(float min, float max)
        {
            _fixedMin = min;
            _fixedMax = max;
            _autoScale = false;
        }

        private int GetIndex(int offset) => (_head - _count + offset + _capacity) % _capacity;

        private void RecalculateMinMax()
        {
            _minValue = float.MaxValue;
            _maxValue = float.MinValue;
            for (int i = 0; i < _count; i++)
            {
                float v = _values[GetIndex(i)];
                if (v < _minValue) _minValue = v;
                if (v > _maxValue) _maxValue = v;
            }
            float range = _maxValue - _minValue;
            if (range > 0) { _minValue -= range * 0.05f; _maxValue += range * 0.05f; }
            else { _minValue -= 1f; _maxValue += 1f; }
        }
    }
}
