import value from '../assets/scss/_themes-vars.module.scss';

// 숫자 확인
const hasNumber = (value) => {
    return new RegExp(/[0-9]/).test(value);
};

// 대소문자 확인
const hasMixed = (value) => {
    return new RegExp(/[a-z]/).test(value) && new RegExp(/[A-Z]/).test(value);
};

// 특수문자 확인
const hasSpecial = (value) => {
    return new RegExp(/[!#@$%^&*)(+=._-]/).test(value);
};

// 패스워드 강함 정도를 색깔로 표시
export const strengthColor = (count) => {
    if (count < 2) return { label: '나쁨', color: value.errorMain };
    if (count < 3) return { label: '약함', color: value.warningDark };
    if (count < 4) return { label: '보통', color: value.orangeMain };
    if (count < 5) return { label: '좋음', color: value.successMain };
    if (count < 6) return { label: '강함', color: value.successDark };
};

// password strength indicator
export const strengthIndicator = (value) => {
    let strengths = 0;
    if (value.length > 5) strengths++;
    if (value.length > 7) strengths++;
    if (hasNumber(value)) strengths++;
    if (hasSpecial(value)) strengths++;
    if (hasMixed(value)) strengths++;
    return strengths;
};