from typing import List, Tuple

# Derived from snippet in CODE-BASE-3.txt of the Unity 6000.0.48f1 codebase:
#     if ((word)b <= (word)old->r_end && (word)e >= (word)old->r_start) {
#         if ((word)b < (word)old->r_start) { old->r_start = b; }
#         if ((word)e > (word)old->r_end) { old->r_end = e; }
#     }


def merge_intervals(intervals: List[Tuple[int, int]]) -> List[Tuple[int, int]]:
    """Merge overlapping intervals and return the result."""
    if not intervals:
        return []
    intervals.sort()
    merged = [list(intervals[0])]
    for b, e in intervals[1:]:
        last = merged[-1]
        if b <= last[1]:
            last[1] = max(last[1], e)
        else:
            merged.append([b, e])
    return [tuple(m) for m in merged]
