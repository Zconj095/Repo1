"""Script to fix duplicate options in NEAT configuration file."""

import configparser
from pathlib import Path
import os
import shutil

def fix_neat_config():
    """Remove duplicate options from NEAT config file."""
    # Get the directory where this script is located
    script_dir = Path(__file__).parent
    config_path = script_dir / "neat_config.ini"
    
    print(f"🔍 Looking for config file at: {config_path.absolute()}")
    print(f"📁 Current working directory: {os.getcwd()}")
    
    # Try different possible locations
    possible_paths = [
        config_path,
        Path("neat_config.ini"),
        Path("quantum_evo/neat_config.ini"),
        script_dir.parent / "neat_config.ini"
    ]
    
    found_config = None
    for path in possible_paths:
        print(f"🔍 Checking: {path.absolute()}")
        if path.exists():
            found_config = path
            print(f"✅ Found config at: {path.absolute()}")
            break
    
    if not found_config:
        print("❌ neat_config.ini not found in any expected location")
        print("📂 Files in current directory:")
        for file in Path(".").iterdir():
            if file.is_file():
                print(f"   - {file.name}")
        return
    
    config_path = found_config
    
    # Read the config file manually to identify duplicates
    with open(config_path, 'r') as f:
        lines = f.readlines()
    
    print(f"📄 Config file has {len(lines)} lines")
    
    # Track sections and options to remove duplicates
    seen_sections = set()
    sections = {}
    current_section = None
    clean_lines = []
    duplicates_found = 0
    duplicate_sections_found = 0
    
    for line_num, line in enumerate(lines, 1):
        original_line = line
        line = line.strip()
        
        # Skip empty lines and comments
        if not line or line.startswith('#') or line.startswith(';'):
            clean_lines.append(original_line)
            continue
        
        # Section header
        if line.startswith('[') and line.endswith(']'):
            section_name = line[1:-1]
            
            # Check for duplicate sections
            if section_name in seen_sections:
                print(f"⚠️ Removing duplicate section '{section_name}' at line {line_num}")
                duplicate_sections_found += 1
                current_section = None  # Skip this section entirely
                continue
            else:
                seen_sections.add(section_name)
                current_section = section_name
                sections[current_section] = set()
                clean_lines.append(original_line)
                continue
        
        # Skip lines if we're in a duplicate section
        if current_section is None:
            continue
        
        # Option line
        if '=' in line and current_section:
            option_name = line.split('=')[0].strip()
            
            # Check if we've seen this option before in this section
            if option_name in sections[current_section]:
                print(f"⚠️ Removing duplicate '{option_name}' in section '{current_section}' at line {line_num}")
                duplicates_found += 1
                continue
            else:
                sections[current_section].add(option_name)
                clean_lines.append(original_line)
        else:
            clean_lines.append(original_line)
    
    total_issues = duplicates_found + duplicate_sections_found
    
    if total_issues == 0:
        print("✅ No duplicates found in configuration file")
        return
    
    # Create a unique backup filename
    backup_counter = 1
    backup_path = config_path.with_suffix('.ini.backup')
    while backup_path.exists():
        backup_path = config_path.with_suffix(f'.ini.backup_{backup_counter}')
        backup_counter += 1
    
    # Copy original to backup (don't rename in case we need to restore)
    shutil.copy2(config_path, backup_path)
    print(f"📁 Backed up original config to {backup_path}")
    
    # Write the cleaned config
    with open(config_path, 'w') as f:
        f.writelines(clean_lines)
    
    print(f"✅ Fixed NEAT configuration file:")
    print(f"   - Removed {duplicates_found} duplicate options")
    print(f"   - Removed {duplicate_sections_found} duplicate sections")
    
    # Verify the config can be loaded
    try:
        config = configparser.ConfigParser()
        config.read(config_path)
        print("✅ Configuration file validation passed")
        print(f"📊 Found sections: {list(config.sections())}")
        
        # Show some key sections
        required_sections = ['NEAT', 'DefaultGenome', 'DefaultSpeciesSet', 'DefaultStagnation', 'DefaultReproduction']
        missing_sections = [s for s in required_sections if s not in config.sections()]
        if missing_sections:
            print(f"⚠️ Missing required sections: {missing_sections}")
        else:
            print("✅ All required sections present")
            
    except Exception as e:
        print(f"❌ Configuration file still has issues: {e}")
        print("🔄 Restoring original config file...")
        
        # Restore from backup
        shutil.copy2(backup_path, config_path)
        print("✅ Original config restored")
        
        # Suggest creating a clean config instead
        print("\n💡 Suggestion: The config file may be too corrupted.")
        print("   Would you like to create a clean config file instead? (y/n)")

def create_clean_config():
    """Create a minimal, working NEAT configuration."""
    
    config_content = """[NEAT]
fitness_criterion     = max
fitness_threshold     = 3.9
pop_size              = 150
reset_on_extinction   = False

[DefaultGenome]
# node activation options
activation_default      = sigmoid
activation_mutate_rate  = 0.0
activation_options      = sigmoid

# node aggregation options
aggregation_default     = sum
aggregation_mutate_rate = 0.0
aggregation_options     = sum

# node bias options
bias_init_mean          = 0.0
bias_init_stdev         = 1.0
bias_max_value          = 30.0
bias_min_value          = -30.0
bias_mutate_power       = 0.5
bias_mutate_rate        = 0.7
bias_replace_rate       = 0.1

# genome compatibility options
compatibility_disjoint_coefficient = 1.0
compatibility_weight_coefficient   = 0.5

# connection add/remove rates
conn_add_prob           = 0.5
conn_delete_prob        = 0.5

# connection enable options
enabled_default         = True
enabled_mutate_rate     = 0.01

feed_forward            = True
initial_connection      = full

# node add/remove rates
node_add_prob           = 0.2
node_delete_prob        = 0.2

# network parameters
num_hidden              = 0
num_inputs              = 4
num_outputs             = 1

# node response options
response_init_mean      = 1.0
response_init_stdev     = 0.0
response_max_value      = 30.0
response_min_value      = -30.0
response_mutate_power   = 0.0
response_mutate_rate    = 0.0
response_replace_rate   = 0.0

# connection weight options
weight_init_mean        = 0.0
weight_init_stdev       = 1.0
weight_max_value        = 30
weight_min_value        = -30
weight_mutate_power     = 0.5
weight_mutate_rate      = 0.8
weight_replace_rate     = 0.1

[DefaultSpeciesSet]
compatibility_threshold = 3.0

[DefaultStagnation]
species_fitness_func = max
max_stagnation       = 20
species_elitism      = 2

[DefaultReproduction]
elitism            = 2
survival_threshold = 0.2
"""
    
    # Create backup of existing file if it exists
    config_path = Path("neat_config.ini")
    if config_path.exists():
        backup_counter = 1
        backup_path = config_path.with_suffix('.ini.old')
        while backup_path.exists():
            backup_path = config_path.with_suffix(f'.ini.old_{backup_counter}')
            backup_counter += 1
        shutil.copy2(config_path, backup_path)
        print(f"📁 Backed up existing config to {backup_path}")
    
    with open('neat_config.ini', 'w') as f:
        f.write(config_content)
    
    print("✅ Created clean NEAT configuration file")

if __name__ == "__main__":
    print("🔧 NEAT Config Fixer")
    print("=" * 40)
    
    choice = input("Choose option:\n1. Fix existing config\n2. Create clean config\nEnter choice (1 or 2): ").strip()
    
    if choice == "2":
        create_clean_config()
    else:
        fix_neat_config()
        
    print("\n🎯 You can now run your main.py script!")