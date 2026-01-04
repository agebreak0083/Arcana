# OpenAI API 설정 가이드

## 개요
이 프로젝트는 OpenAI API 키를 별도 파일로 관리하여 Git에 업로드되지 않도록 합니다.

## 설정 방법

### 1. 설정 파일 생성
`Assets/Scripts/openai_config.example.json` 파일을 복사하여 `openai_config.json` 파일을 생성하세요.

```bash
# Windows (PowerShell)
Copy-Item Assets\Scripts\openai_config.example.json Assets\Scripts\openai_config.json

# Mac/Linux
cp Assets/Scripts/openai_config.example.json Assets/Scripts/openai_config.json
```

### 2. API 키 입력
생성된 `openai_config.json` 파일을 열고 실제 API 키 값을 입력하세요:

```json
{
  "apiKey": "sk-your-actual-api-key-here",
  "assistantId": "asst-your-assistant-id-here",
  "threadId": "thread-your-thread-id-here"
}
```

### 3. Git에서 제외 확인
`openai_config.json` 파일은 `.gitignore`에 추가되어 있어 Git에 업로드되지 않습니다.

## 파일 구조
- `openai_config.example.json`: 예시 파일 (Git에 포함됨)
- `openai_config.json`: 실제 설정 파일 (Git에 포함되지 않음)

## 주의사항
- **절대 `openai_config.json` 파일을 Git에 커밋하지 마세요!**
- API 키가 노출되면 보안 문제가 발생할 수 있습니다.
- 새로운 개발자가 프로젝트에 참여할 때는 `openai_config.example.json`을 복사하여 자신의 API 키를 입력해야 합니다.
