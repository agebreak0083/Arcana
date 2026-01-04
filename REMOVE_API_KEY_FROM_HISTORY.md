# Git 히스토리에서 API 키 제거 가이드

## 문제
GitHub Push Protection이 Git 히스토리에 남아있는 API 키를 감지하여 push를 차단하고 있습니다.

## 해결 방법

### 방법 1: git filter-branch 사용 (권장)

```bash
# 1. API 키를 히스토리에서 제거
git filter-branch --force --index-filter \
  "git rm --cached --ignore-unmatch Assets/Scripts/AIAdvisorIRIS.cs Assets/Scripts/Python/AIAdvisorIRIS.py Assets/Scenes/IntroScene.unity" \
  --prune-empty --tag-name-filter cat -- --all

# 2. API 키 문자열을 빈 문자열로 교체
git filter-branch --force --tree-filter \
  'if [ -f Assets/Scripts/AIAdvisorIRIS.cs ]; then
     sed -i "s/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//g" Assets/Scripts/AIAdvisorIRIS.cs
   fi' \
  --prune-empty --tag-name-filter cat -- --all

# 3. Force push (주의: 협업 중이면 팀원과 상의 필요)
git push origin --force --all
git push origin --force --tags
```

### 방법 2: BFG Repo-Cleaner 사용 (더 빠름)

```bash
# 1. BFG 다운로드 (https://rtyley.github.io/bfg-repo-cleaner/)
# 2. API 키를 포함한 파일 삭제
java -jar bfg.jar --delete-files AIAdvisorIRIS.cs
java -jar bfg.jar --delete-files AIAdvisorIRIS.py
java -jar bfg.jar --delete-files IntroScene.unity

# 3. 또는 특정 문자열 제거
java -jar bfg.jar --replace-text passwords.txt

# passwords.txt 내용:
# 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==>REPLACED

# 4. 정리
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# 5. Force push
git push origin --force --all
```

### 방법 3: 새 브랜치에서 시작 (가장 안전)

```bash
# 1. 현재 상태에서 새 브랜치 생성
git checkout -b main-clean

# 2. API 키가 제거된 상태로 새 커밋
git add .
git commit -m "Remove all API keys"

# 3. 원격 저장소에 새 브랜치 push
git push origin main-clean

# 4. GitHub에서 main 브랜치를 main-clean으로 교체
```

## 주의사항

⚠️ **Force push는 위험합니다!**
- 협업 중인 프로젝트라면 팀원들과 반드시 상의하세요
- Force push 후 다른 팀원들은 `git pull --rebase` 또는 저장소를 다시 클론해야 합니다
- 백업을 먼저 생성하세요: `git clone --mirror <repo-url> backup.git`

## 권장 사항

가장 안전한 방법은 **방법 3 (새 브랜치)**입니다. 
GitHub에서 새 브랜치를 기본 브랜치로 설정하면 히스토리를 건드리지 않고도 문제를 해결할 수 있습니다.
