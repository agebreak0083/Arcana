#!/bin/bash
# Git 히스토리에서 API 키 제거 스크립트

API_KEY="YOUR_API_KEY_HERE"

echo "⚠️  주의: 이 스크립트는 Git 히스토리를 수정합니다!"
echo "백업을 먼저 생성하세요: git clone --mirror <repo-url> backup.git"
read -p "계속하시겠습니까? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]
then
    exit 1
fi

echo "Git 히스토리에서 API 키 제거 중..."

# git filter-repo가 설치되어 있는지 확인
if command -v git-filter-repo &> /dev/null; then
    echo "git-filter-repo를 사용합니다..."
    git filter-repo --replace-text <(echo "$API_KEY==>REMOVED") --force
else
    echo "git-filter-repo가 설치되어 있지 않습니다."
    echo "git filter-branch를 사용합니다..."
    
    # API 키를 빈 문자열로 교체
    git filter-branch --force --tree-filter '
        if [ -f Assets/Scripts/AIAdvisorIRIS.cs ]; then
            sed -i "s/'"$API_KEY"'//g" Assets/Scripts/AIAdvisorIRIS.cs
        fi
        if [ -f Assets/Scripts/Python/AIAdvisorIRIS.py ]; then
            sed -i "s/'"$API_KEY"'//g" Assets/Scripts/Python/AIAdvisorIRIS.py
        fi
        if [ -f Assets/Scenes/IntroScene.unity ]; then
            sed -i "s/'"$API_KEY"'//g" Assets/Scenes/IntroScene.unity
        fi
    ' --prune-empty --tag-name-filter cat -- --all
fi

echo "✅ 완료! 이제 다음 명령어로 push하세요:"
echo "   git push origin --force --all"
echo "   git push origin --force --tags"
