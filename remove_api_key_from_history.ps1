# Git 히스토리에서 API 키 제거 스크립트 (PowerShell)

$API_KEY = "YOUR_API_KEY_HERE"

Write-Host "⚠️  주의: 이 스크립트는 Git 히스토리를 수정합니다!" -ForegroundColor Yellow
Write-Host "백업을 먼저 생성하세요: git clone --mirror <repo-url> backup.git" -ForegroundColor Yellow
$response = Read-Host "계속하시겠습니까? (y/N)"
if ($response -ne "y" -and $response -ne "Y") {
    exit
}

Write-Host "Git 히스토리에서 API 키 제거 중..." -ForegroundColor Green

# git filter-branch 사용
git filter-branch --force --tree-filter @"
if [ -f Assets/Scripts/AIAdvisorIRIS.cs ]; then
    sed -i 's/$API_KEY//g' Assets/Scripts/AIAdvisorIRIS.cs
fi
if [ -f Assets/Scripts/Python/AIAdvisorIRIS.py ]; then
    sed -i 's/$API_KEY//g' Assets/Scripts/Python/AIAdvisorIRIS.py
fi
if [ -f Assets/Scenes/IntroScene.unity ]; then
    sed -i 's/$API_KEY//g' Assets/Scenes/IntroScene.unity
fi
"@ --prune-empty --tag-name-filter cat -- --all

Write-Host "✅ 완료! 이제 다음 명령어로 push하세요:" -ForegroundColor Green
Write-Host "   git push origin --force --all" -ForegroundColor Cyan
Write-Host "   git push origin --force --tags" -ForegroundColor Cyan
