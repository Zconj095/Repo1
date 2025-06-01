from parser_utils import extract_tokens


def test_extract_tokens(tmp_path):
    p = tmp_path / "sample.c"
    p.write_text("int main() {}\n")
    tokens = extract_tokens(str(p))
    assert "int" in tokens and "main" in tokens
