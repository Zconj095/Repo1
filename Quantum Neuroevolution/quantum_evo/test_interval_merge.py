
from interval_merge import merge_intervals


def test_merge_intervals():
    intervals = [(1, 3), (2, 5), (7, 8)]
    assert merge_intervals(intervals) == [(1, 5), (7, 8)]
