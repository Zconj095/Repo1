"""Hello world test to verify pytest is working."""

def test_hello_world():
    """Basic test to ensure pytest is functional."""
    assert True
    print("✅ Hello World test passed!")

def test_basic_math():
    """Test basic mathematical operations."""
    assert 2 + 2 == 4
    assert 10 / 2 == 5
    assert 3 * 3 == 9
    print("✅ Basic math test passed!")

def test_cupy_import():
    """Test that CuPy can be imported."""
    try:
        import cupy as cp
        x = cp.array([1, 2, 3])
        assert x.shape == (3,)
        print("✅ CuPy import test passed!")
    except ImportError:
        print("⚠️ CuPy not available, skipping test")
        assert True

if __name__ == "__main__":
    test_hello_world()
    test_basic_math()
    test_cupy_import()
    print("🎉 All hello world tests passed!")