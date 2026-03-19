import argparse
import json
import sys
import urllib.error
import urllib.request


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Manual test for /profile/comparecvjob/ endpoint"
    )
    parser.add_argument("--base-url", default="http://127.0.0.1:8000", help="Backend base URL")
    parser.add_argument("--token", required=True, help="JWT access token")
    parser.add_argument("--job-ad-text", help="Job ad text")
    parser.add_argument("--cv-text", help="CV text")
    parser.add_argument("--job-ad-file", help="Path to a text file with job ad content")
    parser.add_argument("--cv-file", help="Path to a text file with CV content")
    parser.add_argument("--top-k", type=int, default=5, help="Top matched jobs for user_ai_response flow")
    parser.add_argument("--min-score", type=float, default=0.2, help="Minimum vector score for user_ai_response flow")
    parser.add_argument("--language", default="English", help="Language for AI response")
    return parser.parse_args()


def read_optional_file(path: str | None) -> str:
    if not path:
        return ""
    with open(path, "r", encoding="utf-8") as file:
        return file.read().strip()


def resolve_text(direct_text: str | None, file_path: str | None) -> str:
    text = (direct_text or "").strip()
    if text:
        return text

    file_text = read_optional_file(file_path)
    return file_text


def main() -> int:
    args = parse_args()

    job_ad_text = resolve_text(args.job_ad_text, args.job_ad_file)
    cv_text = resolve_text(args.cv_text, args.cv_file)

    endpoint = f"{args.base_url.rstrip('/')}/profile/comparecvjob/"
    payload = {"language": args.language}

    if job_ad_text and cv_text:
        payload["jobAdText"] = job_ad_text
        payload["cvText"] = cv_text
    else:
        payload["topK"] = max(1, min(int(args.top_k), 20))
        payload["minScore"] = max(-1.0, min(float(args.min_score), 1.0))

    request = urllib.request.Request(
        endpoint,
        data=json.dumps(payload).encode("utf-8"),
        headers={
            "Authorization": f"Bearer {args.token}",
            "Content-Type": "application/json",
        },
        method="POST",
    )

    try:
        with urllib.request.urlopen(request, timeout=120) as response:
            body = response.read().decode("utf-8")
            print(f"Status: {response.status}")
            try:
                parsed = json.loads(body)
                print(json.dumps(parsed, indent=2, ensure_ascii=False))
            except json.JSONDecodeError:
                print(body)
            return 0
    except urllib.error.HTTPError as error:
        body = error.read().decode("utf-8", errors="replace")
        print(f"HTTP error: {error.code}", file=sys.stderr)
        print(body, file=sys.stderr)
        return 1
    except urllib.error.URLError as error:
        print(f"Connection error: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
