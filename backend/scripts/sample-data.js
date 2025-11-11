#!/usr/bin/env node

const http = require('http');

const API_BASE = 'http://localhost:8080';

// コースデータ
const courses = [
  // Computer Science
  { courseCode: 'CS101', name: 'Introduction to Computer Science', credits: 3, maxCapacity: 50 },
  { courseCode: 'CS102', name: 'Data Structures and Algorithms', credits: 4, maxCapacity: 45 },
  { courseCode: 'CS201', name: 'Database Systems', credits: 3, maxCapacity: 40 },
  { courseCode: 'CS202', name: 'Web Application Development', credits: 3, maxCapacity: 35 },
  { courseCode: 'CS301', name: 'Operating Systems', credits: 4, maxCapacity: 40 },
  { courseCode: 'CS302', name: 'Computer Networks', credits: 3, maxCapacity: 35 },
  { courseCode: 'CS401', name: 'Artificial Intelligence', credits: 4, maxCapacity: 30 },
  { courseCode: 'CS402', name: 'Machine Learning', credits: 4, maxCapacity: 30 },

  // Mathematics
  { courseCode: 'MATH101', name: 'Calculus I', credits: 4, maxCapacity: 60 },
  { courseCode: 'MATH102', name: 'Calculus II', credits: 4, maxCapacity: 55 },
  { courseCode: 'MATH201', name: 'Linear Algebra', credits: 3, maxCapacity: 50 },
  { courseCode: 'MATH202', name: 'Discrete Mathematics', credits: 3, maxCapacity: 45 },
  { courseCode: 'MATH301', name: 'Statistics and Probability', credits: 3, maxCapacity: 40 },

  // English
  { courseCode: 'ENG101', name: 'Technical Writing', credits: 2, maxCapacity: 30 },
  { courseCode: 'ENG201', name: 'Business Communication', credits: 2, maxCapacity: 30 },
  { courseCode: 'ENG301', name: 'Advanced Academic Writing', credits: 3, maxCapacity: 25 },

  // Physics
  { courseCode: 'PHYS101', name: 'Physics I', credits: 4, maxCapacity: 55 },
  { courseCode: 'PHYS102', name: 'Physics II', credits: 4, maxCapacity: 50 },
  { courseCode: 'PHYS201', name: 'Modern Physics', credits: 3, maxCapacity: 35 },

  // Business
  { courseCode: 'BUS101', name: 'Introduction to Business', credits: 3, maxCapacity: 60 },
  { courseCode: 'BUS201', name: 'Marketing Fundamentals', credits: 3, maxCapacity: 50 },
  { courseCode: 'BUS301', name: 'Entrepreneurship', credits: 3, maxCapacity: 30 }
];

// 学生データ
const students = [
  { name: 'Taro Yamada', email: 'taro.yamada@example.com', grade: 1 },
  { name: 'Hanako Tanaka', email: 'hanako.tanaka@example.com', grade: 1 },
  { name: 'Kenji Sato', email: 'kenji.sato@example.com', grade: 2 },
  { name: 'Yuki Suzuki', email: 'yuki.suzuki@example.com', grade: 2 },
  { name: 'Akira Watanabe', email: 'akira.watanabe@example.com', grade: 3 },
  { name: 'Sakura Ito', email: 'sakura.ito@example.com', grade: 3 },
  { name: 'Hiroshi Nakamura', email: 'hiroshi.nakamura@example.com', grade: 4 },
  { name: 'Emi Kobayashi', email: 'emi.kobayashi@example.com', grade: 4 },
  { name: 'Takeshi Kato', email: 'takeshi.kato@example.com', grade: 1 },
  { name: 'Mika Yoshida', email: 'mika.yoshida@example.com', grade: 2 }
];

// 学期データ
const semesters = [
  { year: 2024, period: 'Spring', startDate: '2024-04-01', endDate: '2024-07-31' },
  { year: 2024, period: 'Fall', startDate: '2024-09-01', endDate: '2024-12-31' },
  { year: 2025, period: 'Spring', startDate: '2025-04-01', endDate: '2025-07-31' }
];

const numberEmojis = ['1️⃣', '2️⃣', '3️⃣', '4️⃣', '5️⃣', '6️⃣', '7️⃣', '8️⃣', '9️⃣', '🔟'];

function makeRequest(method, path, data = null) {
  return new Promise((resolve, reject) => {
    const postData = data ? JSON.stringify(data) : null;

    const options = {
      hostname: 'localhost',
      port: 8080,
      path: path,
      method: method,
      headers: {
        'Content-Type': 'application/json'
      }
    };

    if (postData) {
      options.headers['Content-Length'] = Buffer.byteLength(postData);
    }

    const req = http.request(options, (res) => {
      let responseData = '';
      res.on('data', (chunk) => { responseData += chunk; });
      res.on('end', () => {
        try {
          resolve({ status: res.statusCode, data: JSON.parse(responseData) });
        } catch (e) {
          resolve({ status: res.statusCode, data: responseData });
        }
      });
    });

    req.on('error', reject);
    if (postData) {
      req.write(postData);
    }
    req.end();
  });
}

async function main() {
  console.log('🧪 Starting sample data creation...\n');

  // 1. 学期データ作成
  console.log('📅 Creating semesters...\n');
  for (let i = 0; i < semesters.length; i++) {
    const semester = semesters[i];
    console.log(`${i + 1}. Creating ${semester.year} ${semester.period}...`);

    try {
      const result = await makeRequest('POST', '/api/semesters', semester);
      if (result.status === 200 || result.status === 201) {
        console.log(`   ✓ Created successfully`);
      } else if (result.status === 409) {
        console.log(`   ⚠ Already exists (skipping)`);
      } else {
        console.log(`   ⚠ Status ${result.status}`);
      }
    } catch (error) {
      console.error(`   ✗ Error: ${error.message}`);
    }
  }
  console.log('');

  // 2. コースデータ作成
  console.log('📚 Creating courses...\n');
  for (let i = 0; i < courses.length; i++) {
    const course = courses[i];
    const emoji = i < 10 ? numberEmojis[i] : `${i + 1}.`;
    console.log(`${emoji}  Creating ${course.courseCode} - ${course.name}...`);

    try {
      const result = await makeRequest('POST', '/api/courses', course);
      if (result.status === 200 || result.status === 201) {
        console.log(`   ✓ Created successfully`);
      } else if (result.status === 409) {
        console.log(`   ⚠ Already exists (skipping)`);
      } else {
        console.log(`   ⚠ Status ${result.status}`);
      }
    } catch (error) {
      console.error(`   ✗ Error: ${error.message}`);
    }
  }
  console.log('');

  // 3. 学生データ作成
  console.log('👥 Creating students...\n');
  for (let i = 0; i < students.length; i++) {
    const student = students[i];
    const emoji = i < 10 ? numberEmojis[i] : `${i + 1}.`;
    console.log(`${emoji}  Creating ${student.name} (Grade ${student.grade})...`);

    try {
      const result = await makeRequest('POST', '/api/students', student);
      if (result.status === 200 || result.status === 201) {
        console.log(`   ✓ Created successfully - ID: ${result.data.studentId || 'N/A'}`);
      } else if (result.status === 409) {
        console.log(`   ⚠ Already exists (skipping)`);
      } else {
        console.log(`   ⚠ Status ${result.status}`);
      }
    } catch (error) {
      console.error(`   ✗ Error: ${error.message}`);
    }
  }
  console.log('');

  // 4. サマリー表示
  console.log('✅ Sample data creation completed!\n');

  console.log('📊 Summary:');
  console.log(`   Semesters: ${semesters.length} items`);
  console.log(`   Courses:   ${courses.length} items`);
  console.log(`   Students:  ${students.length} items`);
  console.log('');

  // 5. データ一覧取得
  console.log('🔍 Fetching created data...\n');

  try {
    console.log('📚 All Courses:');
    const coursesResult = await makeRequest('GET', '/api/courses');
    if (coursesResult.status === 200) {
      console.log(JSON.stringify(coursesResult.data, null, 2));
    }
    console.log('');

    console.log('👥 All Students:');
    const studentsResult = await makeRequest('GET', '/api/students');
    if (studentsResult.status === 200) {
      console.log(JSON.stringify(studentsResult.data, null, 2));
    }
    console.log('');

    console.log('📅 All Semesters:');
    const semestersResult = await makeRequest('GET', '/api/semesters');
    if (semestersResult.status === 200) {
      console.log(JSON.stringify(semestersResult.data, null, 2));
    }
  } catch (error) {
    console.error(`Error fetching data: ${error.message}`);
  }

  console.log('\n🎉 All done! You can now explore the API at http://localhost:8080/index.html');
}

main().catch(console.error);
