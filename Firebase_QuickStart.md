# Firebase 설정 빠른 시작 가이드

## 🚀 5분 안에 시작하기

### 1단계: Firebase 프로젝트 생성 (2분)

1. https://console.firebase.google.com/ 접속
2. "프로젝트 추가" 클릭
3. 프로젝트 이름 입력 → "프로젝트 만들기"
4. 좌측 메뉴: "Realtime Database" → "데이터베이스 만들기"
5. 위치: `asia-southeast1` 선택
6. "테스트 모드로 시작" 선택

### 2단계: Unity 앱 등록 (1분)

1. Firebase Console 프로젝트 개요 → Unity 아이콘 클릭
2. 패키지 이름 입력: `com.yourcompany.arcana`
3. `google-services.json` 다운로드
4. **중요**: 파일을 `Assets/` 폴더에 복사

### 3단계: Firebase SDK 설치 (2분)

1. https://firebase.google.com/download/unity 에서 SDK 다운로드
2. Unity: `Assets` → `Import Package` → `Custom Package`
3. `FirebaseDatabase.unitypackage` 선택 → Import

### 4단계: 씬 설정 (30초)

1. Hierarchy에서 빈 오브젝트 생성 → 이름: `FirebaseManager`
2. `FirebaseManager` 스크립트 추가
3. Play 버튼 클릭하여 테스트

---

## ✅ 확인 사항

### Firebase Console에서 확인
- [ ] Realtime Database가 생성되었는가?
- [ ] 보안 규칙이 설정되었는가?
- [ ] Unity 앱이 등록되었는가?

### Unity 프로젝트에서 확인
- [ ] `google-services.json`이 Assets 폴더에 있는가?
- [ ] FirebaseDatabase.unitypackage가 임포트되었는가?
- [ ] FirebaseManager 오브젝트가 씬에 있는가?

### 테스트
- [ ] Play 모드에서 "Firebase 초기화 성공!" 로그가 보이는가?
- [ ] Tactics 저장 시 Firebase에 데이터가 저장되는가?

---

## 📊 데이터 구조

**Firebase Database 경로**: `/tactics/{username}_{timestamp}`

**예시 키**: `agebreak-wo2_2512061905`

**저장되는 데이터**:
```json
{
  "username": "agebreak-wo2",
  "timestamp": "2025-12-06 19:05:00",
  "tacticsJson": "{...tactics.json 전체 내용...}"
}
```

---

## 🔧 자주 발생하는 문제

### "Firebase가 초기화되지 않았습니다"
→ `google-services.json`이 Assets 폴더에 있는지 확인

### "DependencyStatus.UnavailableOther"
→ `Assets` → `External Dependency Manager` → `Force Resolve`

### 데이터가 저장되지 않음
→ Firebase Console에서 보안 규칙 확인 (테스트 모드로 설정)

---

## 📖 상세 가이드

전체 설정 가이드는 `Firebase_Setup_Guide.md` 파일을 참고하세요.

---

**빠른 테스트 방법**:
1. Unity Play 모드 실행
2. Tactics 화면에서 편성 변경
3. "Run Battle" 클릭
4. Firebase Console → Realtime Database → 데이터 탭에서 확인
