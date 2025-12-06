# GitHub 푸시 오류 해결 가이드

## 문제: Firebase SDK 파일이 너무 커서 GitHub에 푸시할 수 없음

Firebase SDK의 일부 파일들이 100MB를 초과하여 GitHub의 파일 크기 제한에 걸렸습니다.

## 해결 방법

### 방법 1: Git 히스토리에서 큰 파일 제거 (권장)

#### 1단계: BFG Repo-Cleaner 다운로드 (가장 쉬운 방법)

```powershell
# Chocolatey가 설치되어 있다면
choco install bfg-repo-cleaner

# 또는 수동 다운로드
# https://rtyley.github.io/bfg-repo-cleaner/
# bfg.jar 파일 다운로드
```

#### 2단계: 큰 파일 제거

```powershell
# 프로젝트 디렉토리에서 실행
cd c:\Project\Arcana

# 100MB 이상 파일 모두 제거
java -jar bfg.jar --strip-blobs-bigger-than 100M .

# Git 히스토리 정리
git reflog expire --expire=now --all
git gc --prune=now --aggressive
```

#### 3단계: 강제 푸시

```powershell
git push origin main --force
```

---

### 방법 2: Git Filter-Branch 사용 (BFG 없이)

```powershell
# 특정 파일 패턴 제거
git filter-branch --force --index-filter `
  "git rm --cached --ignore-unmatch -r Assets/Firebase/Plugins/x86_64/*.so Assets/Firebase/Plugins/x86_64/*.bundle" `
  --prune-empty --tag-name-filter cat -- --all

# 히스토리 정리
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# 강제 푸시
git push origin main --force
```

---

### 방법 3: 새로운 저장소로 시작 (가장 간단하지만 히스토리 손실)

```powershell
# 1. 현재 .git 폴더 백업
cd c:\Project\Arcana
Move-Item .git .git_backup

# 2. 새로운 Git 저장소 초기화
git init
git add .
git commit -m "Initial commit without Firebase SDK"

# 3. GitHub 저장소에 강제 푸시
git remote add origin https://github.com/agebreak0083/Arcana.git
git push origin main --force
```

---

## 현재 상태 확인

### .gitignore 업데이트 완료 ✅
Firebase SDK 파일들이 .gitignore에 추가되었습니다:
- `Assets/Firebase/`
- `Assets/ExternalDependencyManager/`

### README.md 생성 완료 ✅
Firebase SDK 설치 방법이 포함된 README가 생성되었습니다.

---

## 권장 순서

1. **방법 1 (BFG)** 시도 - 가장 빠르고 안전
2. 실패 시 **방법 2 (Filter-Branch)** 시도
3. 모두 실패 시 **방법 3 (새 저장소)** 사용

---

## 주의사항

⚠️ **강제 푸시 (--force) 사용 시 주의**
- 다른 사람이 이미 클론한 저장소가 있다면 문제가 될 수 있습니다
- 혼자 작업하는 프로젝트라면 안전합니다
- 팀 프로젝트라면 팀원들에게 미리 알려야 합니다

⚠️ **히스토리 손실**
- 방법 1, 2는 큰 파일만 제거하고 나머지 히스토리는 유지
- 방법 3은 모든 히스토리가 손실됨

---

## 다음 단계

푸시 성공 후:
1. 팀원들에게 Firebase SDK 설치 방법 공유 (README.md 참고)
2. `Firebase_Setup_Guide.md` 파일 확인
3. 각자 Firebase SDK 다운로드 및 설치

---

## 문제 해결

### "error: failed to push" 계속 발생
→ Git 히스토리에 여전히 큰 파일이 남아있음
→ 방법 3 (새 저장소) 사용 권장

### BFG 실행 오류
→ Java가 설치되어 있는지 확인
→ `java -version` 명령어로 확인

### Filter-Branch 느림
→ 저장소가 크면 시간이 오래 걸릴 수 있음
→ BFG 사용 권장 (훨씬 빠름)

---

**작성일**: 2025-12-06
